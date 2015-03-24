using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SurfaceMachineSystems.QcController.MotorCommands.MessageEntry
{
    public class CPLAll : SurfaceMachineSystems.QcController.QcMessageEntry
    {
        public CPLAll(QcMessage? Datagram = null)
            : base(Datagram)
        {
            TxMessage = new QcMessage()
            {
                Command = QcCommand.POR,
                Type = PacketType.Command,
                MotorAddress = MotorAddress,
                Psw = new List<QcPsw>(),
                Data = new List<int>(new [] { 65535 })
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
