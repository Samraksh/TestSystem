using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace TestRig
{
    class PwrUSBWrapper
    {
        // Main Functions
        [DllImport("PwrUSBDll.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        public static extern int InitPowerUSB(out int mode, StringBuilder firmwareVersion);

        [DllImport("PwrUSBDll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ClosePowerUSB();

        [DllImport("PwrUSBDll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetCurrentPowerUSB(int count);

        [DllImport("PwrUSBDll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int CheckStatusPowerUSB();

        [DllImport("PwrUSBDll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetPortPowerUSB(int port1_power, int port2_power, int port3_power);

        [DllImport("PwrUSBDll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetDefaultStatePowerUSB(int state1, int state2, int port3);

        [DllImport("PwrUSBDll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ReadPortStatePowerUSB(out int state1, out int state2, out int state3);

        [DllImport("PwrUSBDll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ReadDefaultPortStatePowerUSB(out int state1, out int state2, out int state3);

        [DllImport("PwrUSBDll.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        public static extern int GetFirmwareVersionPowerUSB(StringBuilder firmwareVersion);


        // Current Sensing and Reset functions
        [DllImport("PwrUSBDll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ReadCurrentPowerUSB(out int current);

        [DllImport("PwrUSBDll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ReadCurrentCumPowerUSB(out int currentCum);

        [DllImport("PwrUSBDll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ResetCurrentCounterPowerUSB();

        [DllImport("PwrUSBDll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetCurrentSensRatioPowerUSB(int currentRatio);

        [DllImport("PwrUSBDll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetOverloadPowerUSB(int overload);

        [DllImport("PwrUSBDll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetOverloadPowerUSB();

        [DllImport("PwrUSBDll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ResetBoard();

        [DllImport("PwrUSBDll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetCurrentOffset();


        // Digital IO functions. Available only when Digital IO model of PowerUSB is connected
        [DllImport("PwrUSBDll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetIODirectionPowerUSB(int[] direction);

        [DllImport("PwrUSBDll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetOutputStatePowerUSB(int[] output);

        [DllImport("PwrUSBDll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetInputStatePowerUSB(int[] input);

        [DllImport("PwrUSBDll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int GenerateClockPowerUSB(int port, int onTime, int offTime);

        [DllImport("PwrUSBDll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetOutputStatePowerUSB(int[] output);

        [DllImport("PwrUSBDll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetInputTriggerPowerUSB(int port, int outlet1, int outlet2, int outlet3, int out1, int out2);

        // Watchdog related functions. Available only when Watchdog model of PowerUSB is connected
        [DllImport("PwrUSBDll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int StartWatchdogTimerPowerUSB(int HbTimeSec, int numHbMisses, int resetTimeSec);

        [DllImport("PwrUSBDll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int StopWatchdogTimerPowerUSB();

        [DllImport("PwrUSBDll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetWatchdogStatusPowerUSB();

        [DllImport("PwrUSBDll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int SendHeartBeatPowerUSB();

        [DllImport("PwrUSBDll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int PowerCyclePowerUSB(int resetTimeSec);
    }
}
