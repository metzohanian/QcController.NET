using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SurfaceMachineSystems.QcController.MotorCommands.MessageEntry
{
    public class CPL : SurfaceMachineSystems.QcController.QcMessageEntry
    {
        public CPL(QcMessage? Datagram = null)
            : base(Datagram)
        {
            TxMessage = new QcMessage()
            {
                Command = QcCommand.CPL,
                Type = PacketType.Command,
                MotorAddress = MotorAddress,
                Psw = new List<QcPsw>(),
                Data = Datagram.HasValue?new List<int>():Datagram.Value.Data
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
