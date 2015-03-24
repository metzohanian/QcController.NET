using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SurfaceMachineSystems.QcController.MotorCommands.MessageCommands
{
    public class CorrectableCommand : SurfaceMachineSystems.QcController.MessageCommand
    {

        public CorrectableCommand(QcMessage Datagram)
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
