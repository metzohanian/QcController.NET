using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SurfaceMachineSystems.QcController
{
    public class MessageCommand
    {
        protected QcMessage TxMessage;
        protected bool Complete;
        private int MotorAddress;
        protected bool Emitted;

        public int Address
        {
            get
            {
                return TxMessage.MotorAddress;
            }
            set
            {
                TxMessage.MotorAddress = value;
            }
        }

        public MessageCommand(QcMessage Datagram)
        {
            Datagram.Type = QcPacketType.Command;
            if (Datagram.Data == null)
                Datagram.Data = new List<int>();
            Datagram.MotorAddress = MotorAddress;
            if (Datagram.Psw == null)
                Datagram.Psw = new List<QcPsw>();
            TxMessage = Datagram;
            Complete = false;
            Emitted = false;
        }

        public virtual QcMessage EmitTxMessage(RunMode OperationMode)
        {
            if (!Emitted && Datagram.IsEmittingRunMode(OperationMode))
            {
                Emitted = true;
                return TxMessage;
            }
            return new QcMessage()
            {
                Command = TxMessage.Command,
                Data = TxMessage.Data,
                Type = QcPacketType.NoMessage,
                MotorAddress = TxMessage.MotorAddress,
                Psw = TxMessage.Psw
            };
        }

        public bool IsComplete()
        {
            return Complete;
        }

        public void Terminate()
        {
            Complete = true;
        }

        public virtual void UpdateStatus(QcMessage Datagram)
        {
            if ((Datagram.Type == QcPacketType.Ack || (Datagram.Type == QcPacketType.Data && Datagram.Command == TxMessage.Command)) && Emitted)
                Complete = true;
        }
    }
}
