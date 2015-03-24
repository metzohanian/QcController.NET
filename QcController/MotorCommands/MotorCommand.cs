using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SurfaceMachineSystems.QcController
{

    public delegate void MotorCommandProgressHandler(int CurrentMsg, int TotalMessages, RunMode Status);

    public class MotorCommand
    {
        public event MotorCommandProgressHandler MotorCommandProgress = delegate { };

        protected List<MessageCommand> TxMessages;
        protected int CurrentTxMessage;
        protected int TxMessagedEmitted;
        protected RunMode OperationMode;

        private int MotorAddress;
        private bool CompleteTriggered;

        public UserCallBackParameters FlightParameters;

        public int Address
        {
            set
            {
                MotorAddress = value;
                foreach (MessageCommand mc in TxMessages)
                    mc.Address = value;
            }
            get
            {
                return MotorAddress;
            }
        }

        public MotorCommand(MessageCommand Command, UserCallBackParameters FlightParameters)
        {
            Command.Address = MotorAddress;
            TxMessages = new List<MessageCommand>();
            TxMessages.Add(Command);
            CompleteTriggered = false;
            TxMessagedEmitted = -1;
            OperationMode = RunMode.Waiting;
            this.FlightParameters = FlightParameters;
        }

        public virtual QcMessage EmitTxMessage(RunMode OperationMode)
        {
            this.OperationMode = OperationMode;
            if (CurrentTxMessage < TxMessages.Count)
            {
                TxMessagedEmitted = CurrentTxMessage;
                QcMessage emit = TxMessages[CurrentTxMessage].EmitTxMessage(OperationMode);
                if (emit.Type != QcPacketType.NoMessage)
                {
                    foreach (Delegate action in MotorCommandProgress.GetInvocationList())
                    {
                        ((MotorCommandProgressHandler)action).BeginInvoke(CurrentTxMessage, TxMessages.Count, RunMode.Emitting, null, null);
                        ((MotorCommandProgressHandler)action).BeginInvoke(CurrentTxMessage, TxMessages.Count, RunMode.Running, null, null);
                    }
                }
                return emit;
            }
            return new QcMessage() { Type = QcPacketType.NoMessage };
        }

        public bool IsComplete()
        {
            return CurrentTxMessage >= TxMessages.Count;
        }

        public void Terminate()
        {
            if (CurrentTxMessage < TxMessages.Count)
            {
                TxMessages[CurrentTxMessage].Terminate();
            }
            IncrementStatus();
        }

        protected void IncrementStatus()
        {
            CurrentTxMessage++;
            if (CurrentTxMessage >= TxMessages.Count && !CompleteTriggered)
            {
                CompleteTriggered = true;
                foreach (Delegate action in MotorCommandProgress.GetInvocationList())
                {
                    ((MotorCommandProgressHandler)action).BeginInvoke(CurrentTxMessage, TxMessages.Count, RunMode.Complete, null, null);
                }
            }
            else if (OperationMode == RunMode.Paused)
            {
                foreach (Delegate action in MotorCommandProgress.GetInvocationList())
                {
                    ((MotorCommandProgressHandler)action).BeginInvoke(CurrentTxMessage, TxMessages.Count, RunMode.Paused, null, null);
                }
            }
        }

        public virtual void UpdateStatus(QcMessage Datagram)
        {
            TxMessages[CurrentTxMessage].UpdateStatus(Datagram);
            if (TxMessages[CurrentTxMessage].IsComplete())
            {
                IncrementStatus();
            }
        }
    }
}
