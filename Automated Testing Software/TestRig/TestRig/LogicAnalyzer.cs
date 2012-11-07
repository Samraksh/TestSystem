using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SaleaeDeviceSdkDotNet;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.IO;
using System.Threading;

namespace TestRig
{
    public class LogicAnalyzer
    {
        private UInt32 mSampleRateHz;
        private MLogic mLogic;
        private MLogic16 mLogic16;

        private MSaleaeDevices devices = new MSaleaeDevices();

        private System.IO.StreamWriter file;
        //private MemoryMappedFile mmf;
        //private MemoryMappedViewAccessor accessor;
        //private long accessorOffset;
        //private long accessorLength;
        private long sampleNumber;
        private byte previousSample;
        private byte bitMask;        

        public LogicAnalyzer(int sampleFrequency, string projectName)
        {
            mSampleRateHz = (UInt32)sampleFrequency;
            devices = new MSaleaeDevices();
            devices.OnLogicConnect += new MSaleaeDevices.OnLogicConnectDelegate(devices_LogicOnConnect);
            devices.OnLogic16Connect += new MSaleaeDevices.OnLogic16ConnectDelegate(devices_Logic16OnConnect);
            devices.OnDisconnect += new MSaleaeDevices.OnDisconnectDelegate(devices_OnDisconnect);
            devices.BeginConnect();

            setBitMask(projectName);

            // wait for device to be found; sleep for 1 second
            // TODO: figure out a way to detect that we can use logic instead of just waiting
            Thread.Sleep(2000);
        }

        private bool setBitMask(string hkpFile)
        {
            try
            {
                using (StreamReader reader = new StreamReader(hkpFile))
                {
                    bitMask = 0x00;
                    string line;
                    line = reader.ReadLine();  // NumLine

                    line = reader.ReadLine();
                    while (line != null)
                    {
                        if (line.Contains(":"))
                        {
                            string[] parsedLine = line.Split(':');
                            string groupName = parsedLine[0].Trim();
                            string value = parsedLine[1];
                            value = value.Replace(';', ' ');
                            value = value.Trim();
                            switch (int.Parse(value))
                            {
                                case 1:
                                    bitMask |= 0x01;
                                    break;
                                case 2:
                                    bitMask |= 0x02;
                                    break;
                                case 3:
                                    bitMask |= 0x04;
                                    break;
                                case 4:
                                    bitMask |= 0x08;
                                    break;
                                case 5:
                                    bitMask |= 0x10;
                                    break;
                                case 6:
                                    bitMask |= 0x20;
                                    break;
                                case 7:
                                    bitMask |= 0x40;
                                    break;
                                case 8:
                                    bitMask |= 0x80;
                                    break;
                            }
                        }
                        line = reader.ReadLine();
                    }
                    
                    System.Diagnostics.Debug.WriteLine("bitMask: " + bitMask.ToString());
                }
                if (bitMask == 0x00)
                    return false;
                else
                    return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Logic: setBitMask fail: " + ex.Message);
                return false;
            }
        }

        public bool Initialize(int sampleFrequency, string projectName)
        {
            mSampleRateHz = (UInt32)sampleFrequency;
            setBitMask(projectName);
            if (mLogic != null) mLogic.SampleRateHz = mSampleRateHz;
            if (mLogic16 != null) mLogic16.SampleRateHz = mSampleRateHz;                        

            return true;
        }

        public bool startMeasure(string filename, int sampleMs)
        {
            if (filename == "")
                return false;

            //if (mLogic != null) accessorLength = (int)((sampleMs / 1000F) * (float)mSampleRateHz * 16F);  // 8 bytes + 8 chars of ',' or '\0'
            //else accessorLength = (int)((sampleMs / 1000F) * (float)mSampleRateHz * 32F);

            if (filename == "") return false;
            file = new System.IO.StreamWriter(filename, false);
            System.Diagnostics.Debug.WriteLine("Logic: opened file " + filename);
            //mmf = System.IO.MemoryMappedFiles.MemoryMappedFile.CreateFromFile(filename, FileMode.OpenOrCreate, "csv", accessorLength);
            //accessor = mmf.CreateViewAccessor(0, accessorLength);

            //accessorOffset = 0;
            sampleNumber = 0;
            
            if (mLogic != null)
            {
                System.Diagnostics.Debug.WriteLine("Logic: ReadStart()");
                mLogic.ReadStart();
            }
            if (mLogic16 != null)
            {
                //System.Diagnostics.Debug.WriteLine("calling 16 ReadStart()");
                MainWindow.showMessageBox("16 bit logic analyzer not supported yet.");
                mLogic16.ReadStart();
                return false;
            }

            return true;
        }

        public bool stopMeasure()
        {
            if (mLogic != null) mLogic.Stop();
            if (mLogic16 != null) mLogic16.Stop();

            file.Close();
            System.Diagnostics.Debug.WriteLine("Logic: StopMeasure; Closed file"); 
            //accessor.Dispose();
            //mmf.Dispose();            

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
            mLogic16.OnReadData += new MLogic16.OnReadDataDelegate(mLogic16_OnReadData);
            mLogic16.OnError += new MLogic16.OnErrorDelegate(mLogic_OnError);
            mLogic16.SampleRateHz = mSampleRateHz;
        }

        void devices_OnDisconnect(ulong device_id)
        {
            if (mLogic != null) mLogic = null;
            if (mLogic16 != null) mLogic16 = null;
            System.Diagnostics.Debug.WriteLine("OnDisconnect()");
        }

        void mLogic_OnReadData(ulong device_id, byte[] data)
        {
            try
            {
                int i;
                //string writeStr;
                //byte[] asciiBytes;

                System.Diagnostics.Debug.WriteLine("Logic: Read {0} bytes, starting with 0x{1:X}", data.Length, (ushort)data[0]);

                for (i = 0; i < data.Length; i++)
                {
                    if ((sampleNumber == 0) || ((data[i] & bitMask) != previousSample))
                    {
                        previousSample = (byte)(data[i] & bitMask);                        
                        file.Write(sampleNumber.ToString());
                        if ((bitMask & 0x80) != 0) file.Write("," + ((char)(((data[i] & 0x80) >> 7) + '0')).ToString() );
                        if ((bitMask & 0x40) != 0) file.Write("," + ((char)(((data[i] & 0x40) >> 6) + '0')).ToString());
                        if ((bitMask & 0x20) != 0) file.Write("," + ((char)(((data[i] & 0x20) >> 5) + '0')).ToString());
                        if ((bitMask & 0x10) != 0) file.Write("," + ((char)(((data[i] & 0x10) >> 4) + '0')).ToString());
                        if ((bitMask & 0x08) != 0) file.Write("," + ((char)(((data[i] & 0x08) >> 3) + '0')).ToString());
                        if ((bitMask & 0x04) != 0) file.Write("," + ((char)(((data[i] & 0x04) >> 2) + '0')).ToString());
                        if ((bitMask & 0x02) != 0) file.Write("," + ((char)(((data[i] & 0x02) >> 1) + '0')).ToString());
                        if ((bitMask & 0x01) != 0) file.Write("," + ((char)(((data[i] & 0x01)) + '0')).ToString());
                        file.WriteLine("");
                        //writeStr = sampleNumber.ToString() + "," + ((char)((data[i] & 0x01) + '0')).ToString();
                        //asciiBytes = Encoding.ASCII.GetBytes(writeStr);
                        //accessor.WriteArray(accessorOffset, asciiBytes, 0, asciiBytes.Length);
                        //accessorOffset += asciiBytes.Length;
                        //accessor.Write(accessorOffset++, '\n');
                    }

                    sampleNumber++;
                }
            }
            catch (Exception)
            {
                System.Diagnostics.Debug.WriteLine("Logic Analyzer is attempting to write to a closed file");
            }
        }
        void mLogic16_OnReadData(ulong device_id, byte[] data)
        {

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
