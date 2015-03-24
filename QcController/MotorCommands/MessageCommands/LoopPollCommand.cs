using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SurfaceMachineSystems.QcController.MotorCommands.MessageCommands
{
    public class LoopPollCommand : SurfaceMachineSystems.QcController.MessageCommand
    {
        DateTime NextPoll;

        public LoopPollCommand(QcMessage Datagram)
            : base(Datagram)
        {
            NextPoll = DateTime.Now;
        }

        public override QcMessage EmitTxMessage(RunMode OperationMode)
        {
            if (!Datagram.IsEmittingRunMode(OperationMode) && OperationMode != RunMode.Paused)
            {
                return new QcMessage()
                {
                    Command = TxMessage.Command,
                    Data = TxMessage.Data,
                    Type = QcPacketType.NoMessage,
                    MotorAddress = TxMessage.MotorAddress,
                    Psw = TxMessage.Psw
                };
            }
            else if (DateTime.Now >= NextPoll)
            {
                Controller.ColorReport("Emit Loop Poll", ConsoleColor.Cyan, ConsoleColor.DarkBlue);
                NextPoll = DateTime.Now.AddMilliseconds(35);
                Emitted = true;
                return TxMessage;
            }
            else
            {
                return new QcMessage()
                {
                    Command = TxMessage.Command,
                    MotorAddress = TxMessage.MotorAddress,
                    Type = QcPacketType.NoMessage,
                    Psw = new List<QcPsw>(),
                    Data = new List<int>()
                };
            }
        }

        public override void UpdateStatus(QcMessage Datagram)
        {
            if (Datagram.Type == QcPacketType.Data && (Datagram.Command == QcCommand.POL || Datagram.Command == QcCommand.POR) && Emitted == true && Datagram.Psw.Contains(QcPsw.PCmdDone))
            {
                Complete = true;
            }
        }
    }
}
