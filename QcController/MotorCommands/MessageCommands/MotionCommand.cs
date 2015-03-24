using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SurfaceMachineSystems.QcController.MotorCommands.MessageCommands
{
    public class MotionCommand : SurfaceMachineSystems.QcController.MessageCommand
    {
        public MotionCommand(QcMessage Datagram)
            : base(Datagram)
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