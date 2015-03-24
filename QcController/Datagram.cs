using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SurfaceMachineSystems.QcController
{
   
    public enum QcPacketType
    {
        Ack,
        Nak,
        Data,
        Command,
        Unknown,
        BadFormat,
        NoMessage
    }

    public enum QcCommand
    {
        RIS = 20, 
        CIS = 163, 
        HLT = 2, 
        RSP = 255, 
        RST = 4, 
        POL = 0, 
        CPL = 1, 
        POR = 27, 
        RRG = 12, 
        MRV = 135, 
        MRT = 177, 
        ZTP = 145,
        ZTG = 144,
        MAV = 134,
        HSM = 229
    }

    public enum QcHaltMode
    {
        Ignore,
        HaltMotor,
        HaltAllMotors
    }

    public enum QcPsw
    {
        ICmdDone        = 0x8000,   // 15
        NvMemError      = 0x4000,   // 14
        PCmdDone        = 0x2000,   // 13
        CommandError    = 0x1000,   // 12
        InputFound      = 0x0800,   // 11
        LowOverVolt     = 0x0400,   // 10
        HoldingError    = 0x0200,   // 9
        MovingError     = 0x0100,   // 8
        RxOverflow      = 0x0080,   // 7
        CksCondMet      = 0x0040,   // 6
        MsgTooLong      = 0x0020,   // 5
        FramingError    = 0x0010,   // 4
        KillMotor       = 0x0008,   // 3
        SoftLimit       = 0x0004,   // 2
        RxChecksum      = 0x0002,   // 1
        AbortedPkt      = 0x0001,   // 0

        ClearAll        = 0xFFFF,
        ClearCommon     = 0X2010
    }

    public enum QcNak
    {
        NoError = 0,
        BadCommand = 1,
        DeviceBusy = 2,
        Reserved = 3,
        Reserved2 = 4,
        BadFormat = 5,
        BufferFull = 6,
        BadAddress = 7,
        BadResponsePacketRequest = 8,
        BadPUPLockoutCode = 9,
        BadChecksum = 10
    }

    public enum RunMode
    {
        Waiting,
        Emitting,
        Running,
        Paused,
        Complete,
        Halted
    }

    public struct QcMessage
    {
        public QcPacketType Type;
        public int MotorAddress;
        public QcCommand Command;
        public List<int> Data;
        public List<QcPsw> Psw;
        public List<QcNak> Nak;
    }

    public struct QcMotorConfiguration
    {
        public static QcMotorConfiguration Default = new QcMotorConfiguration()
        {
            MaxRpm = 4000,
            CountsPerRev = 8000,
            RevPerInch = 2
        };
        public int MaxRpm;
        public int CountsPerRev;
        public int RevPerInch;
        public double LengthMm;
        public double Time;
        public void SetDefaults()
        {
            MaxRpm = Default.MaxRpm;
            CountsPerRev = Default.CountsPerRev;
            RevPerInch = Default.RevPerInch;
        }
        public int Svu
        {
            get
            {
                return (int)(((2 * LengthMm / 25.4) / Time) * SvuRps);
            }
        }
        public int Counts
        {
            get
            {
                return (int)(((CountsPerRev * RevPerInch) / 25.4) * LengthMm);
            }
        }
        public double SvuRps
        {
            get
            {
                return (Math.Pow(2, 31) / MaxRpm) * (60);
            }
        }
    }

    public class Datagram
    {
        public static bool IsEmittingRunMode(RunMode OperationMode) {
            switch (OperationMode)
            {
                case RunMode.Complete: return false;
                case RunMode.Halted: return false;
                case RunMode.Paused: return false;
                case RunMode.Running: return true;
                case RunMode.Waiting: return true;
                default:
                    return false;
            }
        }

        public static string TranslateCommand(List<QcMessage> Message)
        {
            string[] e = new string[Message.Count];
            for (int m = 0; m < Message.Count; m++)
            {
                e[m] = TranslateCommand(Message[m]);
            }
            return string.Join("\r", e);
        }

        public static string TranslateCommand(QcMessage Message)
        {
            List<string> l = Message.Data == null?new List<string>():new List<string>(Message.Data.Select(i => i.ToString()).ToArray());
            return TranslateCommand(Message.MotorAddress, Message.Command, l);
        }

        public static string TranslateCommand(int MotorAddress, QcCommand Command, List<string> Parameters = null)
        {
            string motorcommand = "@" + MotorAddress + " " + ((int)Command).ToString() + " ";
            if (Parameters != null && Parameters.Count > 0)
                motorcommand += " " + string.Join(" ", Parameters.ToArray());
            return motorcommand;
        }

        public static List<QcNak> ParseNak(int iNak)
        {
            List<QcNak> Nak = new List<QcNak>();
            foreach (QcNak nak in Enum.GetValues(typeof(QcNak)))
            {
                if (iNak == (int)nak)
                    Nak.Add(nak);
            }
            return Nak;
        }

        public static List<QcPsw> ParsePSW(int iPSW)
        {
            List<QcPsw> Psw = new List<QcPsw>();
            foreach (QcPsw psw in Enum.GetValues(typeof(QcPsw)))
            {
                if ((iPSW & (int)psw) > 0)
                    Psw.Add(psw);
            }
            if (iPSW == 0) Psw.Add(QcPsw.ICmdDone);
            Psw.Remove(QcPsw.ClearAll);
            Psw.Remove(QcPsw.ClearCommon);
            return Psw;
        }

        public static bool CommandComplete(List<QcPsw> psw)
        {
            return psw.Contains(QcPsw.PCmdDone);
        }

        public static bool PswError(List<QcPsw> psw, bool IsRecoverable = false)
        {
            bool HasError = false;
            bool Recoverable = true;
            foreach (QcPsw c in psw)
            {
                switch (c)
                {
                    case QcPsw.ICmdDone: break;
                    case QcPsw.NvMemError: HasError |= true; Recoverable &= false; break;
                    case QcPsw.PCmdDone: break;
                    case QcPsw.CommandError: HasError |= true; Recoverable &= false; break;
                    case QcPsw.InputFound: break;
                    case QcPsw.LowOverVolt: HasError |= true; Recoverable &= false; break;
                    case QcPsw.HoldingError: HasError |= true; Recoverable &= false; break;
                    case QcPsw.MovingError: HasError |= true; Recoverable &= false; break;
                    case QcPsw.RxOverflow: HasError |= true; Recoverable &= false; break;
                    case QcPsw.CksCondMet: break;
                    case QcPsw.MsgTooLong: HasError |= true; Recoverable &= false; break;
                    case QcPsw.FramingError: HasError |= true; Recoverable &= true; break;
                    case QcPsw.KillMotor: HasError |= true; Recoverable &= false; break;
                    case QcPsw.SoftLimit: break;
                    case QcPsw.RxChecksum: HasError |= true; Recoverable &= false; break;
                    case QcPsw.AbortedPkt: HasError |= true; Recoverable &= false; break;
                }
            }
            if (IsRecoverable && HasError && Recoverable)
                return true;
            return HasError;
        }

        public static QcMessage ParseResponse(string Response)
        {
            QcMessage r = new QcMessage() { Data = new List<int>(), Psw = new List<QcPsw>() };
            string[] components = Response.Split(new char[] { ' ' });
            switch (components[0][0])
            {
                case '*':
                    r.Type = QcPacketType.Ack;
                    r.MotorAddress = Convert.ToInt32(components[1], 16);
                    break;
                case '#':
                    r.Type = QcPacketType.Data;
                    r.MotorAddress = Convert.ToInt32(components[1], 16);
                    if (components.Length > 3)
                    {
                        r.Command = (QcCommand)Convert.ToInt32(components[2], 16);
                        for (int i = 3; i < components.Length; i++)
                            r.Data.Add(Convert.ToInt32(components[i], 16));
                        if (r.Command == 0)
                            r.Psw = ParsePSW(r.Data[0]);
                    }
                    else
                    {
                        r.Type = QcPacketType.BadFormat;
                    }
                    break;
                case '!':
                    r.Type = QcPacketType.Nak;
                    r.MotorAddress = Convert.ToInt32(components[1], 16);
                    if (components.Length == 4)
                    {
                        r.Command = (QcCommand)Convert.ToInt32(components[2], 16);
                        for (int i = 3; i < components.Length; i++)
                            r.Data.Add(Convert.ToInt32(components[i], 16));
                        r.Nak = ParseNak(r.Data[0]);
                    }
                    else
                    {
                        r.Type = QcPacketType.BadFormat;
                    }
                    break;
                case '@':
                    r.Type = QcPacketType.Command;
                    r.MotorAddress = Convert.ToInt32(components[1], 10);
                    if (components.Length == 4)
                    {
                        r.Command = (QcCommand)Convert.ToInt32(components[2], 10);
                        for (int i = 3; i < components.Length; i++)
                            r.Data.Add(Convert.ToInt32(components[i], 10));
                    }
                    else
                    {
                        r.Type = QcPacketType.BadFormat;
                    }
                    break;
                default:
                    r.Type = QcPacketType.Unknown;
                    break;
            }
            return r;
        }

    }
}
