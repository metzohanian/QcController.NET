using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SurfaceMachineSystems.QcController.MotorCommands
{
    public class ImmediateMotorCommand : SurfaceMachineSystems.QcController.MotorCommand
    {
        public ImmediateMotorCommand(MessageCommand Command, UserCallBackParameters FlightParameters)
            : base(Command, FlightParameters)
        {
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
