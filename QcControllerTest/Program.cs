using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SurfaceMachineSystems.QcController;

namespace SurfaceMachineSystems
{
    class Program
    {
        static DateTime Start;
        static System.Timers.Timer HaltTimer;
        static QcController.Controller md;

        static void Main(string[] args)
        {
            md = new QcController.Controller();
            List<string> ports = md.ComPorts();
            HaltTimer = new System.Timers.Timer();
            HaltTimer.Enabled = false;
            HaltTimer.Interval = 1400;
            HaltTimer.Elapsed += new System.Timers.ElapsedEventHandler(HaltTimer_Elapsed);
            HaltTimer.AutoReset = false;

            if (ports.Contains("COM1"))
            {
                /*
                int max_rpm = 4000;
                int counts_per_rev = 4000;
                int rev_per_inch = 2;

                double svu_rps = (Math.Pow(2, 31) / max_rpm) * (60);

                double length_mm = 100;
                double time = 0.25;

                int cycles = (int)(((counts_per_rev * rev_per_inch) / 25.4) * length_mm);
                double rps = (2 * length_mm / 25.4) / time;

                int svu = (int)(svu_rps * rps);
                */

                QcMotorConfiguration mconf = new QcMotorConfiguration();
                mconf.SetDefaults();
                mconf.LengthMm = 100;
                mconf.Time = 0.25;

                md.PortName = "COM1";
                md.Progress += new QcCommandProgressHandler(md_Progress);
                md.InitializeMotor(16);

                Controller.StatusReportMode = QcStatusReportMode.Console;

                //md.LoadMotorHaltRecover(16);
                int direction = 1;
                for (int i = 0; i < 8; i++)
                {
                    md.AddMotionCommand(16, QcCommand.MRV, new List<int>(new int[] { direction * mconf.Counts, 80000, mconf.Svu, 0, 0 }), new UserCallBackParameters());
                    md.AddCoordinatedMotion(new List<Tuple<int,QcCommand,List<int>>>(new Tuple<int,QcCommand,List<int>>[] {
                        new Tuple<int,QcCommand,List<int>>(16, QcCommand.MRV, new List<int>(new int[] {mconf.Counts, 8000000, 1014559203, 0, 0 })), 
                        new Tuple<int,QcCommand,List<int>>(17, QcCommand.MRV, new List<int>(new int[] {mconf.Counts, 8000000, 1014559203, 0, 0 }))
                    }), new UserCallBackParameters());
                    direction *= -1;
                }
                Start = DateTime.Now;
                HaltTimer.Enabled = false;
                md.StartCommands();
            }
            else
            {
                Console.WriteLine("Port 1 does not exist.");
            }
            Console.ReadKey();
        }

        static void HaltTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            DateTime Halt = DateTime.Now;
            md.Pause(16);
            int cycles = 31496;
            for (int i = 0; i < 12; i++)
            {
                md.AddMotionCommand(16, QcCommand.MRV, new List<int>(new int[] { cycles, 8000000, 1014559203, 0, 0 }));
                cycles *= -1;
            }
            HaltTimer.Enabled = false;
            Controller.ColorReport("Halt in: " + (DateTime.Now - Halt).TotalMilliseconds, ConsoleColor.Red, ConsoleColor.DarkRed);
        }

        static void md_Progress(int Motor, int Command, int TotalCommands, RunMode Mode)
        {
            bool NonEmitting = false;
            if (Mode == RunMode.Running && Command == 0)
            {
                Controller.ColorReport("Motor Command Progress: [" + Motor + ":" + Command + "/" + TotalCommands + ":" + Mode.ToString() + "] " + (DateTime.Now - Start).TotalMilliseconds, ConsoleColor.White, ConsoleColor.DarkGray);
            }
            if (!SurfaceMachineSystems.QcController.Datagram.IsEmittingRunMode(Mode))
            {
                if (Mode == RunMode.Paused)
                {
                    NonEmitting = true;
                }
                else
                {
                    Controller.ColorReport("Motor Command Progress: [" + Motor + ":" + Command + "/" + TotalCommands + ":" + Mode.ToString() + "] " + (DateTime.Now - Start).TotalMilliseconds, ConsoleColor.White, ConsoleColor.DarkGray);
                }
            }
            if (NonEmitting)
            {
                Controller.ColorReport("Motor Command Progress:[" + Motor + ":" + Command + "/" + TotalCommands + ":" + Mode.ToString() + "] " + (DateTime.Now - Start).TotalMilliseconds, ConsoleColor.White, ConsoleColor.DarkGray);
                System.Threading.Thread.Sleep(1000);
                md.Resume();
                NonEmitting = false;
            }
        }
    }
}

/*


 */