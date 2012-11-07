using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SaleaeDeviceSdkDotNet;

namespace TestRig
{
    public class LogicAnalyser
    {
        UInt32 mSampleRateHz = 4000000;
        //MLogic mLogic;
        //MLogic16 mLogic16;
        byte mWriteValue = 0;

        public LogicAnalyser()
        {
            MSaleaeDevices devices = new MSaleaeDevices();
           // devices.OnLogicConnect += new MSaleaeDevices.OnLogicConnectDelegate(devices_LogicOnConnect);
           // devices.OnLogic16Connect += new MSaleaeDevices.OnLogic16ConnectDelegate(devices_Logic16OnConnect);
           // devices.OnDisconnect += new MSaleaeDevices.OnDisconnectDelegate(devices_OnDisconnect);
           // devices.BeginConnect();
           // Console.WriteLine("Logic is currently set up to read and write at {0} Hz.  You can change this in the code.", mSampleRateHz);
        }

        /*
        void devices_LogicOnConnect(ulong device_id, MLogic logic)
        {
        }

        void devices_Logic16OnConnect(ulong device_id, MLogic16 logic_16)
        {
        }

        void devices_OnDisconnect(ulong device_id)
        {
        }
         */
    }
}
