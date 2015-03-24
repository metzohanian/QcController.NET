using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SurfaceMachineSystems.QcController
{

    public delegate void CommandProgressHandler(int Motor, int Command, int TotalCommands, RunMode Mode);

    public class MotorControl
    {
        public event CommandProgressHandler CommandProgress = delegate { };

        private int MotorAddress;
        public List<MotorCommand> Commands;
        private int CurrentCommand;
        private bool enabled;
        private bool FirstEmit;
        private bool LastEmit;

        public int Current
        {
            get
            {
                return CurrentCommand;
            }
        }

        public int CommandCount
        {
            get
            {
                return Commands.Count;
            }
        }

        public MotorControl(int address)
        {
            MotorAddress = address;
            CleanUp();
        }

        public void CleanUp()
        {
            Commands = new List<MotorCommand>();
            CurrentCommand = 0;
            enabled = true;
            FirstEmit = true;
            LastEmit = false;
        }

        public int AddCommand(MotorCommand command)
        {
            command.Address = MotorAddress;
            lock (Commands)
            {
                Commands.Add(command);
                Commands.Last().MotorCommandProgress += new MotorCommandProgressHandler(MotorControl_MotorCommandProgress);
                return Commands.Count() - 1;
            }
        }

        void MotorControl_MotorCommandProgress(int CurrentMsg, int TotalMessages, RunMode Status)
        {
            CommandProgress(MotorAddress, CurrentCommand, Commands.Count, Status);
        }

        public bool Enabled
        {
            get { return enabled; }
            set { enabled = value; }
        }

        public QcMessage EmitTxMessage(RunMode OperationMode)
        {
            if (enabled && CurrentCommand < Commands.Count)
            {
                if (CurrentCommand == 0 && FirstEmit)
                {
                    LastEmit = true;
                    FirstEmit = false;
                    OperationMode = RunMode.Running;
                    CommandProgress(MotorAddress, -1, Commands.Count, RunMode.Running);
                }
                return Commands[CurrentCommand].EmitTxMessage(OperationMode);
            }
            OperationMode = RunMode.Waiting;
            FirstEmit = true;
            CommandProgress(MotorAddress, -1, Commands.Count, RunMode.Waiting);
            return new QcMessage() { Type = QcPacketType.NoMessage };
        }

        public void Terminate()
        {
            if (CurrentCommand < Commands.Count)
            {
                Commands[CurrentCommand].Terminate();
                CurrentCommand++;
            }
        }

        public bool IsComplete()
        {
            if (CurrentCommand >= Commands.Count && LastEmit)
            {
                LastEmit = false;
                CommandProgress(MotorAddress, -1, Commands.Count, RunMode.Complete);
            }
            return CurrentCommand >= Commands.Count;
        }

        public void UpdateStatus(QcMessage Datagram)
        {
            if (Datagram.Data == null)
                Datagram.Data = new List<int>();
            if (Datagram.Psw == null)
                Datagram.Psw = new List<QcPsw>();

            if (CurrentCommand < Commands.Count)
            {
                Commands[CurrentCommand].UpdateStatus(Datagram);
                if (Commands[CurrentCommand].IsComplete())
                    CurrentCommand++;
            }
        }
    }
}
