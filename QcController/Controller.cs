using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;

namespace SurfaceMachineSystems.QcController
{
    /**************************************************************
     
        QcController

        MessageDispatch
	        Sends Messages
	        Receives and Parses Messages
	        Dispatches Message to each Motor Queue
	
        MotorQueue
	        Triggered by polling and message events
	        Works on each Command in Order
	        Command Process Modes
		        Immediate
		        Motion Control
		        Retransmit
     
     *************************************************************/

    public delegate void QcCommandProgressHandler(int Motor, int Command, int TotalCommands, RunMode Mode);
    public delegate void QcCommandUserCallBackHandler(int Motor, int Command, int TotalCommands, RunMode Mode, object Parameters);
    public delegate void QcStatusReportHandler(string Report);

    public enum QcStatusReportMode
    {
        Console,
        Event
    }

    public class UserCallBackParameters
    {
        public object PreFlight;
        public object PostFlight;

        public UserCallBackParameters(object PreFlight, object PostFlight = null)
        {
            this.PreFlight = PreFlight;
            this.PostFlight = PostFlight;
        }

        public UserCallBackParameters()
        {

        }
    }

    public class Controller
    {
        public static event QcStatusReportHandler StatusReport = delegate { };
        public static QcStatusReportMode StatusReportMode = QcStatusReportMode.Console;

        public event QcCommandProgressHandler Progress = delegate { };
        public event QcCommandUserCallBackHandler PreFlight = delegate { };
        public event QcCommandUserCallBackHandler PostFlight = delegate { };

        private SerialPort Port;
        public string Buffer;

        private List<string> Responses;
        private int LastUnprocessedResponse = 0;

        private System.Timers.Timer CommunicationTimer;

        private Dictionary<int, MotorControl> Motors;
        private Dictionary<int, RunMode> ProgressRaised;
        private Dictionary<int, RunMode> Operation;

        public Controller()
        {
            Port = new SerialPort();
            Port.BaudRate = 57600;
            Port.StopBits = StopBits.Two;
            Port.DataBits = 8;
            Port.NewLine = "\r";

            Port.DataReceived += new SerialDataReceivedEventHandler(Port_DataReceived);

            Responses = new List<string>();

            Progress += Controller_Progress;

            Buffer = "";

            Motors = new Dictionary<int, MotorControl>();
            lock (Motors)
            {
                ProgressRaised = new Dictionary<int, RunMode>();
                Operation = new Dictionary<int, RunMode>();

                CommunicationTimer = new System.Timers.Timer();
                CommunicationTimer.Enabled = false;
                CommunicationTimer.Interval = 500;
                CommunicationTimer.Elapsed += new System.Timers.ElapsedEventHandler(CommunicationTimer_Elapsed);
            }
        }

        public int BaudRate
        {
            set
            {
                Port.BaudRate = value;
            }
            get
            {
                return Port.BaudRate;
            }


        }
        public StopBits StopBits
        {
            get
            {
                return Port.StopBits;
            }
            set
            {
                Port.StopBits = value;
            }
        }
        public int DataBits
        {
            get
            {
                return Port.DataBits;
            }
            set
            {
                Port.DataBits = value;
            }
        }
        public string NewLine
        {
            get
            {
                return Port.NewLine;
            }
            set
            {
                Port.NewLine = value;
            }
        }

        void Controller_Progress(int Motor, int Command, int TotalCommands, RunMode Mode)
        {
            if (Mode == RunMode.Complete && Command <= TotalCommands)
            {
                if (Motors.ContainsKey(Motor) && Motors[Motor].Commands.Count > Command && Command > 0)
                {
                    PostFlightTrigger(Motor, Command, TotalCommands, Mode, Motors[Motor].Commands[Command--].FlightParameters.PostFlight);
                }
            }
            if (Mode == RunMode.Emitting && Command < TotalCommands)
            {
                if (Motors.ContainsKey(Motor) && Motors[Motor].Commands.Count > Command && Command >= 0)
                {
                    PreFlightTrigger(Motor, Command, TotalCommands, Mode, Motors[Motor].Commands[Command].FlightParameters.PreFlight);
                }
            }
        }
        
        ~Controller()
        {
            if (Port.IsOpen)
            {
                Port.DiscardInBuffer();
                Port.Close();
                Port.Dispose();
            }
        }

        public void LoadMotorHaltRecover(int MotorAddress)
        {
            AddImmediateCommand(MotorAddress, QcCommand.POL);
            AddImmediateCommand(MotorAddress, QcCommand.CPL, new List<int>(new [] { 65535 }));
            AddImmediateCommand(MotorAddress, QcCommand.RIS);
            AddImmediateCommand(MotorAddress, QcCommand.CIS);
        }

        public Dictionary<int, int> AddCoordinatedMotion(List<Tuple<int, QcCommand, List<int>>> Motion, UserCallBackParameters FlightParameters = null)
        {
            Dictionary<int, int> MotorCommandIds = new Dictionary<int,int>();
            Dictionary<int, QcController.MotorCommands.MessageCommands.SimultaneousMotionCommand> Motions = new Dictionary<int, QcController.MotorCommands.MessageCommands.SimultaneousMotionCommand>();

            foreach (Tuple<int, QcCommand, List<int>> m in Motion)
            {
                Motions.Add(m.Item1, new MotorCommands.MessageCommands.SimultaneousMotionCommand(
                    new QcMessage()
                    {
                        Command = m.Item2,
                        Data = m.Item3
                    }));
            }
            foreach (KeyValuePair<int, MotorCommands.MessageCommands.SimultaneousMotionCommand> s in Motions)
            {
                Dictionary<int, QcController.MotorCommands.MessageCommands.SimultaneousMotionCommand> m = 
                    new Dictionary<int, QcController.MotorCommands.MessageCommands.SimultaneousMotionCommand>();
                m.Add(s.Key, s.Value);

                foreach (KeyValuePair<int, MotorCommands.MessageCommands.SimultaneousMotionCommand> t in Motions.Except(m))
                {
                    s.Value.CoordinatedMotion += new MotorCommands.MessageCommands.CoordinatedMotionHandler(t.Value.Coordinate);
                }
                int commandid = AddMotorCommand(s.Key, new QcController.MotorCommands.MotionControlMotorCommand(s.Value, FlightParameters));
                MotorCommandIds.Add(s.Key, commandid);
            }
            return MotorCommandIds;
        }

        public int AddMotionCommand(int MotorAddress, QcCommand Command, List<int> Parameters, UserCallBackParameters FlightParameters = null)
        {
            return AddMotorCommand(MotorAddress,
                new QcController.MotorCommands.MotionControlMotorCommand(
                    new QcController.MotorCommands.MessageCommands.MotionCommand(
                        new QcMessage()
                        {
                            Command = Command,
                            Data = Parameters
                        }), FlightParameters));
        }

        public int AddImmediateCommand(int MotorAddress, QcCommand Command, List<int> Parameters = null, UserCallBackParameters FlightParameters = null)
        {
            if (Parameters == null)
                Parameters = new List<int>();
            return AddMotorCommand(MotorAddress,
                new QcController.MotorCommands.ImmediateMotorCommand(
                    new QcController.MotorCommands.MessageCommands.ImmediateCommand(
                        new QcMessage()
                        {
                            Command = Command,
                            Data = Parameters
                        }), FlightParameters));
        }

        public void InitializeMotor(int MotorAddress)
        {
            lock (Motors)
            {
                Motors.Add(MotorAddress, new MotorControl(MotorAddress));
                ProgressRaised.Add(MotorAddress, RunMode.Waiting);
                Operation.Add(MotorAddress, RunMode.Waiting);
                Motors[MotorAddress].CommandProgress += new CommandProgressHandler(Controller_CommandProgress);
            }
        }

        void Controller_CommandProgress(int Motor, int Command, int TotalCommands, RunMode Mode)
        {
            Progress(Motor, Command, TotalCommands, Mode);
        }

        public int AddMotorCommand(int MotorAddress, MotorCommand Command)
        {
            lock (Motors)
            {
                Command.Address = MotorAddress;
                if (!Motors.ContainsKey(MotorAddress))
                    InitializeMotor(MotorAddress);
                return Motors[MotorAddress].AddCommand(Command);
            }
        }

        private void SetAllOperations(RunMode OperationMode)
        {
            for (int i = 0; i < Operation.Count; i++)
            {
                Operation[Operation.ElementAt(i).Key] = OperationMode;
            }
        }

        public bool HasCommands()
        {
            int c = 0;
            foreach (KeyValuePair<int, MotorControl> m in Motors)
            {
                c += m.Value.CommandCount;
            }
            return c > 0;
        }

        public void StartCommands()
        {
            lock (Motors)
            {
                SetAllOperations(RunMode.Running);
                CommunicationTimer.Enabled = true;
                RunCommands(null);
            }
        }

        public void Pause(int? MotorAddress = null)
        {
            lock (Motors)
            {
                if (MotorAddress.HasValue)
                {
                    Operation[MotorAddress.Value] = RunMode.Paused;
                }
                else
                {
                    SetAllOperations(RunMode.Paused);
                }
            }
        }

        public void Resume(int? MotorAddress = null)
        {
            lock (Motors)
            {
                if (MotorAddress.HasValue)
                {
                    Operation[MotorAddress.Value] = RunMode.Running;
                }
                else
                {
                    SetAllOperations(RunMode.Running);
                }
            }
            Progress(MotorAddress.HasValue ? MotorAddress.Value : -1, MotorAddress.HasValue ? Motors[MotorAddress.Value].Current : -1, MotorAddress.HasValue ? Motors[MotorAddress.Value].CommandCount : -1, RunMode.Running);
        }

        public void HaltCurrentCommand(int? MotorAddress = null)
        {
            lock (Motors)
            {
                List<QcMessage> Emit = new List<QcMessage>();
                if (MotorAddress.HasValue)
                {
                    Operation[MotorAddress.Value] = RunMode.Halted;
                    Emit.Add(new QcMessage()
                    {
                        MotorAddress = MotorAddress.Value,
                        Command = QcCommand.HLT,
                        Data = new List<int>(),
                        Psw = new List<QcPsw>(),
                        Type = QcPacketType.Command
                    });
                    Motors[(int)MotorAddress].Terminate();
                }
                else
                {
                    SetAllOperations(RunMode.Halted);
                    foreach (KeyValuePair<int, RunMode> op in Operation)
                    {
                        Emit.Add(new QcMessage()
                        {
                            MotorAddress = op.Key,
                            Command = QcCommand.HLT,
                            Data = new List<int>(),
                            Psw = new List<QcPsw>(),
                            Type = QcPacketType.Command
                        });
                    }
                    foreach (KeyValuePair<int, MotorControl> motor in Motors)
                    {
                        Motors[motor.Key].Terminate();
                    }
                }
                WriteLine(Datagram.TranslateCommand(Emit));
            }
            Progress(MotorAddress.HasValue ? MotorAddress.Value : -1, MotorAddress.HasValue ? Motors[MotorAddress.Value].Current : -1, MotorAddress.HasValue ? Motors[MotorAddress.Value].CommandCount : -1, RunMode.Complete);
        }

        public void KillMotorCommands(int? MotorAddress = null)
        {
            lock (Motors)
            {
                List<QcMessage> Emit = new List<QcMessage>();
                if (MotorAddress.HasValue)
                {
                    Operation[MotorAddress.Value] = RunMode.Halted;
                    Emit.Add(new QcMessage()
                    {
                        MotorAddress = MotorAddress.Value,
                        Command = QcCommand.HLT,
                        Data = new List<int>(),
                        Psw = new List<QcPsw>(),
                        Type = QcPacketType.Command
                    });
                    Motors[MotorAddress.Value].CleanUp();
                }
                else
                {
                    SetAllOperations(RunMode.Halted);
                    foreach (KeyValuePair<int, RunMode> op in Operation)
                    {
                        Emit.Add(new QcMessage()
                        {
                            MotorAddress = op.Key,
                            Command = QcCommand.HLT,
                            Data = new List<int>(),
                            Psw = new List<QcPsw>(),
                            Type = QcPacketType.Command
                        });
                    }
                    foreach (KeyValuePair<int, MotorControl> motor in Motors)
                    {
                        motor.Value.CleanUp();
                    }
                }
                WriteLine(Datagram.TranslateCommand(Emit));
            }
            Progress(MotorAddress.HasValue ? MotorAddress.Value : -1, MotorAddress.HasValue ? Motors[MotorAddress.Value].Current : -1, MotorAddress.HasValue ? Motors[MotorAddress.Value].CommandCount : -1, RunMode.Halted);
        }

        private QcMessage IncrementState(int MotorAddress, QcMessage Datagram)
        {
            Motors[MotorAddress].UpdateStatus(Datagram);

            QcMessage emit = Motors[MotorAddress].EmitTxMessage(Operation[MotorAddress]);

            if (Motors[MotorAddress].IsComplete())
            {
                if (ProgressRaised[MotorAddress] != RunMode.Complete)
                {
                    ProgressRaised[MotorAddress] = RunMode.Complete;
                    Progress(MotorAddress, Motors[MotorAddress].Current, Motors[MotorAddress].CommandCount, RunMode.Complete);
                }
                Motors[MotorAddress].CleanUp();
            }

            bool allcomplete = true;
            foreach (KeyValuePair<int, MotorControl> motor in Motors)
            {
                allcomplete &= motor.Value.IsComplete();
            }

            if (allcomplete)
                Progress(-1, -1, -1, RunMode.Complete);

            return emit;
        }

        private void RunCommands(string RxMessage)
        {
            lock (Motors)
            {
                CommunicationTimer.Enabled = false;
                List<QcMessage> Emit = new List<QcMessage>();
                if (RxMessage != null)
                {
                    QcMessage datagram = Datagram.ParseResponse(RxMessage);
                    QcMessage emit = IncrementState(datagram.MotorAddress, datagram);
                    if (emit.Type != QcPacketType.NoMessage)
                        Emit.Add(emit);
                }
                else
                {
                    foreach (KeyValuePair<int, MotorControl> motor in Motors)
                    {
                        QcMessage emit = IncrementState(motor.Key, new QcMessage() { Type = QcPacketType.NoMessage });
                        if (emit.Type != QcPacketType.NoMessage)
                            Emit.Add(emit);
                    }
                }
                if (Emit.Count > 0)
                {
                    WriteLine(Datagram.TranslateCommand(Emit));
                }
                CommunicationTimer.Enabled = true;
            }
        }

        public void PreFlightTrigger(int Motor, int Command, int TotalCommands, RunMode Mode, object PreFlight) {
            this.PreFlight(Motor, Command, TotalCommands, Mode, PreFlight);
        }

        public void PostFlightTrigger(int Motor, int Command, int TotalCommands, RunMode Mode, object PostFlight)
        {
            this.PostFlight(Motor, Command, TotalCommands, Mode, PreFlight);
        }

        public static void ColorReport(string Line, ConsoleColor foreground, ConsoleColor background = ConsoleColor.Black)
        {
            if (StatusReportMode == QcStatusReportMode.Event)
            {
                foreach (Delegate receiver in StatusReport.GetInvocationList())
                {
                    ((QcStatusReportHandler)receiver).BeginInvoke(Line, null, null);
                }
            }
            else if (StatusReportMode == QcStatusReportMode.Console)
            {
                Console.ForegroundColor = foreground;
                Console.WriteLine(Line);
                Console.BackgroundColor = background;
            }
        }

        public string PortName
        {
            get
            {
                return Port.PortName;
            }
            set
            {
                bool IsRunning = CommunicationTimer.Enabled;
                CommunicationTimer.Enabled = false;
                string pname = Port.PortName;
                try
                {
                    if (Port.IsOpen)
                    {
                        Port.Close();
                    }
                    Port.PortName = value;
                    Port.Open();
                    Port.DiscardInBuffer();
                    Port.DiscardOutBuffer();
                }
                catch
                {
                    Port.PortName = pname;
                    Port.Open();
                    Port.DiscardInBuffer();
                    Port.DiscardOutBuffer();
                    throw new Exception("The port " + value + " cannot not be used to operate the Scratch Machine at this time.");
                }
                finally
                {
                    CommunicationTimer.Enabled = IsRunning;
                }
            }
        }

        private void CommunicationTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            RunCommands(null);
        }

        private void WriteLine(string Line)
        {
            lock (Buffer)
            {
                if (!Port.IsOpen)
                {
                    Controller.ColorReport("PORT CLOSED: Tx: " + Line.Replace("\r", "\n\t"), ConsoleColor.Red, ConsoleColor.DarkRed);
                    return;
                }
                while (Port.BytesToWrite > 0)
                    System.Threading.Thread.Sleep(5);
                Port.WriteLine(Line);
                Controller.ColorReport("Tx: " + Line.Replace("\r", "\n\t"), ConsoleColor.Green, ConsoleColor.DarkGreen);
            }
        }

        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            lock (Buffer)
            {
                Buffer += Port.ReadExisting();
                string[] lines = Buffer.Split(new char[1] { '\r' }, StringSplitOptions.RemoveEmptyEntries);
                Responses.AddRange(lines.Take(lines.Length - 1));
                if (Buffer[Buffer.Length - 1] == '\r')
                {
                    Responses.Add(lines[lines.Length - 1]);
                    Buffer = "";
                }
                else
                {
                    Buffer = lines[lines.Length - 1];
                }
                if (Responses.Count >= LastUnprocessedResponse)
                {
                    for (int i = LastUnprocessedResponse; i < Responses.Count; i++)
                    {
                        RunCommands(Responses[LastUnprocessedResponse]);
                        LastUnprocessedResponse++;
                    }
                }
            }
        }

        public List<string> ComPorts()
        {
            return new List<string>(SerialPort.GetPortNames());
        }
    }
}
