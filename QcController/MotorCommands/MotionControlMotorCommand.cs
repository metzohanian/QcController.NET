using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SurfaceMachineSystems.QcController.MotorCommands
{
    public class MotionControlMotorCommand : SurfaceMachineSystems.QcController.MotorCommand
    {
        public MotionControlMotorCommand(MessageCommands.MotionCommand Command, UserCallBackParameters FlightParameters)
            : base(Command, FlightParameters)
        {
            TxMessages.Clear();
            TxMessages.Add(new MessageCommands.ImmediateCommand(new QcMessage() { Command = QcCommand.POL }) { Address = this.Address });
            TxMessages.Add(new MessageCommands.ImmediateCommand(new QcMessage() { Command = QcCommand.CPL, Data = new List<int>(new int[] { (int)QcPsw.ClearAll }) }) { Address = this.Address });
            TxMessages.Add(Command);
            TxMessages.Add(new MessageCommands.LoopPollCommand(new QcMessage() { Command = QcCommand.POL }) { Address = this.Address });
            TxMessages.Add(new MessageCommands.ImmediateCommand(new QcMessage() { Command = QcCommand.CPL, Data = new List<int>(new int[] { (int)QcPsw.PCmdDone }) }) { Address = this.Address });
        }

        public override QcMessage EmitTxMessage(RunMode OperationMode)
        {
            return base.EmitTxMessage(OperationMode);
        }

        public override void UpdateStatus(QcMessage Datagram)
        {
            base.UpdateStatus(Datagram);
        }
    }
}
