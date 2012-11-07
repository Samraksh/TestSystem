using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using SaleaeDeviceSdkDotNet;


namespace TestRig
{
    /*
    class LogicAnalyser
    {

        public LogicAnalyser()
        {
            Initialize();
        }

        [DllImport("LogicAnalyzerDLL.dll", SetLastError = true)]
        static extern void Initialize();
    }
     */
    
    public class LogicAnalyser
    {
        UInt32 mSampleRateHz = 4000000;
        MLogic mLogic;
        MLogic16 mLogic16;

        string filename;

        System.IO.StreamWriter file;

        MSaleaeDevices devices = new MSaleaeDevices();

        public LogicAnalyser()
        {
            devices = new MSaleaeDevices();
            devices.OnLogicConnect += new MSaleaeDevices.OnLogicConnectDelegate(devices_LogicOnConnect);
            devices.OnLogic16Connect += new MSaleaeDevices.OnLogic16ConnectDelegate(devices_Logic16OnConnect);
            devices.OnDisconnect += new MSaleaeDevices.OnDisconnectDelegate(devices_OnDisconnect);
            devices.BeginConnect();
        }

        public bool startMeasure(string filename)
        {
            if (filename == "")
                return false;

            file = new System.IO.StreamWriter(filename);
            System.Diagnostics.Debug.WriteLine("opened file " + filename);
            file.WriteLine("Opened this file");
            if (mLogic != null)
            {
                System.Diagnostics.Debug.WriteLine("calling ReadStart()");
                mLogic.ReadStart();
            }
            if (mLogic16 != null)
            {
                System.Diagnostics.Debug.WriteLine("calling 16 ReadStart()");
                mLogic16.ReadStart();
            }

            return true;
        }

        public bool stopMeasure()
        {
            if (mLogic != null)
                mLogic.Stop();
            else
                mLogic16.Stop();

            file.Close();
            System.Diagnostics.Debug.WriteLine("closed file");
            return true;
        }

        
        void devices_LogicOnConnect(ulong device_id, MLogic logic)
        {
            mLogic = logic;
            mLogic.OnReadData += new MLogic.OnReadDataDelegate(mLogic_OnReadData);
            mLogic.OnWriteData += new MLogic.OnWriteDataDelegate(mLogic_OnWriteData);
            mLogic.OnError += new MLogic.OnErrorDelegate(mLogic_OnError);
            mLogic.SampleRateHz = mSampleRateHz;
            System.Diagnostics.Debug.WriteLine("LogicOnConnect complete");
        }

        void devices_Logic16OnConnect(ulong device_id, MLogic16 logic_16)
        {
            mLogic16 = logic_16;
            logic_16.OnReadData += new MLogic16.OnReadDataDelegate(mLogic16_OnReadData);
            logic_16.OnError += new MLogic16.OnErrorDelegate(mLogic_OnError);
            logic_16.SampleRateHz = mSampleRateHz;
        }

        void devices_OnDisconnect(ulong device_id)
        {
            if (mLogic != null)
                mLogic = null;
            if (mLogic16 != null)
                mLogic16 = null;
            System.Diagnostics.Debug.WriteLine("OnDisconnect()");
        }

        void mLogic_OnReadData(ulong device_id, byte[] data)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Logic: Read {0} bytes, starting with 0x{1:X}", data.Length, (ushort)data[0]);
                /*for (int i = 0; i < data.Length; i++)
                {
                    if (i == (data.Length - 1))
                        file.WriteLine(data[i]);
                    else
                        file.WriteLine(data[i] + ",");
                }*/
            }
            catch (Exception)
            {
                Window1.showMessageBox("Logic Analyzer is attempting to write to a closed file");
            }
        }
        void mLogic16_OnReadData(ulong device_id, byte[] data)
        {
            try
            {                
                for (int i = 0; i < data.Length; i++)
                {
                    if (i == (data.Length - 1))
                        file.WriteLine(data[i]);
                    else
                        file.WriteLine(data[i] + ",");
                }
            }
            catch (Exception)
            {
            }
        }

        void mLogic_OnWriteData(ulong device_id, byte[] data)
        {

        }

        void mLogic_OnError(ulong device_id)
        {
            System.Diagnostics.Debug.WriteLine("Logic Reported an Error.  This probably means that Logic could not keep up at the given data rate, or was disconnected. You can re-start the capture automatically, if your application can tolerate gaps in the data.");
            Console.WriteLine("Logic Reported an Error.  This probably means that Logic could not keep up at the given data rate, or was disconnected. You can re-start the capture automatically, if your application can tolerate gaps in the data.");
        }
    }
     
}
