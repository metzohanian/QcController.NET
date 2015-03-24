using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SurfaceMachineSystems.QcController.MotorCommands.MessageEntry
{
    public class POR : SurfaceMachineSystems.QcController.QcMessageEntry
    {

        public POR(QcMessage? Datagram = null)
            : base(Datagram)
        {
            Complete = false;
            TxMessage = new QcMessage()
            {
                Command = QcCommand.POR,
                Type = PacketType.Command,
                MotorAddress = MotorAddress,
                Psw = new List<QcPsw>()
            };
        }

        public override QcMessage EmitTxMessage()
        {
            return base.EmitTxMessage();
        }

        public override void UpdateStatus(QcMessage Datagram)
        {
            base.UpdateStatus(Datagram);
        }
    }
}
