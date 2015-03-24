using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SurfaceMachineSystems.QcController.MotorCommands.MessageCommands
{
    public delegate void CoordinatedMotionHandler(int ReadyCount);

    public class SimultaneousMotionCommand : SurfaceMachineSystems.QcController.MotorCommands.MessageCommands.MotionCommand
    {
        public event CoordinatedMotionHandler CoordinatedMotion = delegate { };

        int ReadyCount;
        bool ReadyTrip;

        public SimultaneousMotionCommand(QcMessage Datagram)
            : base(Datagram)
        {
            ReadyTrip = false;
            ReadyCount = 0;
        }

        public void Coordinate(int ReadyCount)
        {
            lock (CoordinatedMotion)
            {
                Controller.ColorReport("\t\tCoordinate Rx: " + ReadyCount, ConsoleColor.Black, ConsoleColor.White);
                this.ReadyCount = ReadyCount;
            }
        }

        public override QcMessage EmitTxMessage(RunMode OperationMode)
        {
            int invokers = CoordinatedMotion.GetInvocationList().Count();
            if (ReadyCount >= invokers)
            {
                return base.EmitTxMessage(OperationMode);
            }
            else
            {
                lock (CoordinatedMotion)
                {
                    if (!ReadyTrip)
                    {
                        ReadyCount++;
                        ReadyTrip = true;
                        Controller.ColorReport("\tCoordinate Tx: " + ReadyCount, ConsoleColor.White, ConsoleColor.DarkMagenta);
                        CoordinatedMotion(ReadyCount);
                    }
                }
                return new QcMessage() { Type = QcPacketType.NoMessage };
            }
        }

        public override void UpdateStatus(QcMessage Datagram)
        {
            base.UpdateStatus(Datagram);
        }
    }
}