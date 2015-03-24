using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SurfaceMachineSystems.QcController.MotorCommands.MessageEntry
{
    public class POL : SurfaceMachineSystems.QcController.QcMessageEntry
    {
        public POL(QcMessage? Datagram = null)
            : base(Datagram)
        {
            TxMessage = new QcMessage()
            {
                Command = QcCommand.POL,
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
