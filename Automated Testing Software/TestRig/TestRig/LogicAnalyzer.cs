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
        private bool analyzeI2C;
        private byte lastLevels;
        private byte changeLevels;
        private ushort lastLevels16;
        private ushort changeLevels16;
        private const int I2C_STATE_IDLE = 0;
        private const int I2C_STATE_READING = 1;
        private const int I2C_STATE_POSTBYTE = 2;
        private int i2cState = I2C_STATE_IDLE;
        private byte[] packetData = new byte[1024];
        bool[] packetAck = new bool[1024];
        private byte currentBitPos;
        private byte currentByte;
        bool ignoreGlitch = true;

        public LogicAnalyzer(int sampleFrequency, string projectName)
        {
            mSampleRateHz = (UInt32)sampleFrequency;
            devices = new MSaleaeDevices();
            devices.OnLogicConnect += new MSaleaeDevices.OnLogicConnectDelegate(devices_LogicOnConnect);
            devices.OnLogic16Connect += new MSaleaeDevices.OnLogic16ConnectDelegate(devices_Logic16OnConnect);
            devices.OnDisconnect += new MSaleaeDevices.OnDisconnectDelegate(devices_OnDisconnect);
            devices.BeginConnect();
            
            lastLevels = 0;
            lastLevels16 = 0;
            changeLevels = 0;
            changeLevels16 = 0;
            analyzeI2C = false;

            setBitMask(projectName);

            // wait for device to be found; sleep for 6 second
            Thread.Sleep(6000);
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

                    System.Diagnostics.Debug.WriteLine("bitMask: " + bitMask.ToString("X2"));
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

            ignoreGlitch = true;
            lastLevels = 0;
            lastLevels16 = 0;
            changeLevels = 0;
            changeLevels16 = 0;
            analyzeI2C = false;

            return true;
        }

        public bool startMeasure(string filename, int sampleMs, string testType)
        {
            if ((testType.Equals("I2C") == true) || (testType.Equals("i2c") == true) ){
                analyzeI2C = true;
            } else {
                analyzeI2C = false;
            }
            if (filename == String.Empty)
                return false;

            //if (mLogic != null) accessorLength = (int)((sampleMs / 1000F) * (float)mSampleRateHz * 16F);  // 8 bytes + 8 chars of ',' or '\0'
            //else accessorLength = (int)((sampleMs / 1000F) * (float)mSampleRateHz * 32F);

            if (filename == String.Empty) return false;
            file = new System.IO.StreamWriter(filename, false);
            System.Diagnostics.Debug.WriteLine("Logic: opened file " + filename);
            //mmf = System.IO.MemoryMappedFiles.MemoryMappedFile.CreateFromFile(filename, FileMode.OpenOrCreate, "csv", accessorLength);
            //accessor = mmf.CreateViewAccessor(0, accessorLength);

            //accessorOffset = 0;
            sampleNumber = 0;
            Thread.BeginCriticalRegion();
            if (mLogic != null)
            {
                System.Diagnostics.Debug.WriteLine("Logic: ReadStart()");
                mLogic.ReadStart();
            }
            if (mLogic16 != null)
            {
                System.Diagnostics.Debug.WriteLine("calling 16 ReadStart()");
                //MainWindow.showMessageBox("16 bit logic analyzer not supported yet.");
                mLogic16.ReadStart();
                //return false;
            }

            return true;
        }

        public bool stopMeasure()
        {
            if (mLogic != null) mLogic.Stop();
            if (mLogic16 != null) mLogic16.Stop();
            Thread.EndCriticalRegion();
            file.Close();
            System.Diagnostics.Debug.WriteLine("Logic: StopMeasure; Closed file");
            //accessor.Dispose();
            //mmf.Dispose();            

            return true;
        }

        void devices_LogicOnConnect(ulong device_id, MLogic logic)
        {
            bool sampleRateSupported = false;
            mLogic = logic;
            List<uint> sample_rates = new List<uint>();
            sample_rates = mLogic.GetSupportedSampleRates();
            for (int i = 0; i < sample_rates.Count; ++i)
            {
                if (mSampleRateHz == sample_rates[i])
                    sampleRateSupported = true;                
            }
            if (sampleRateSupported == true)
            {
                mLogic.SampleRateHz = mSampleRateHz;
                System.Diagnostics.Debug.WriteLine("Logic: setting sample rate to: " + mSampleRateHz.ToString());
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Error, unsupported sample rate: " + mSampleRateHz.ToString());
                MainWindow.showMessageBox("Error, unsupported sample rate: " + mSampleRateHz.ToString());
                for (int i = 0; i < sample_rates.Count; ++i)
                {
                    if (mSampleRateHz == sample_rates[i])
                        sampleRateSupported = true;
                    System.Diagnostics.Debug.WriteLine("Logic: supported sample rate: " + sample_rates[i].ToString());
                }
            }
            mLogic.OnReadData += new MLogic.OnReadDataDelegate(mLogic_OnReadData);
            mLogic.OnWriteData += new MLogic.OnWriteDataDelegate(mLogic_OnWriteData);
            mLogic.OnError += new MLogic.OnErrorDelegate(mLogic_OnError);
            mLogic.SampleRateHz = mSampleRateHz;
            System.Diagnostics.Debug.WriteLine("LogicOnConnect complete");
        }

        void devices_Logic16OnConnect(ulong device_id, MLogic16 logic_16)
        {
            bool sampleRateSupported = false;
            mLogic16 = logic_16;
            List<uint> sample_rates = new List<uint>();
            sample_rates = mLogic16.GetSupportedSampleRates();
            for (int i = 0; i < sample_rates.Count; ++i)
            {
                if (mSampleRateHz == sample_rates[i])
                    sampleRateSupported = true;
            }
            if (sampleRateSupported == true)
            {
                mLogic16.SampleRateHz = mSampleRateHz;
                System.Diagnostics.Debug.WriteLine("Logic16: setting sample rate to: " + mSampleRateHz.ToString());
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Error, unsupported sample rate: " + mSampleRateHz.ToString());
                MainWindow.showMessageBox("Error, unsupported sample rate: " + mSampleRateHz.ToString());
                for (int i = 0; i < sample_rates.Count; ++i)
                {
                    if (mSampleRateHz == sample_rates[i])
                        sampleRateSupported = true;
                    System.Diagnostics.Debug.WriteLine("Logic: supported sample rate: " + sample_rates[i].ToString());
                }
            }
            mLogic16.OnReadData += new MLogic16.OnReadDataDelegate(mLogic16_OnReadData);
            mLogic16.OnError += new MLogic16.OnErrorDelegate(mLogic_OnError);
            mLogic16.SampleRateHz = mSampleRateHz;
            System.Diagnostics.Debug.WriteLine("Logic16OnConnect complete");
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
                byte levels;
                int maskSDA = 0x80;
                int maskSCL = 0x40;
                //string writeStr;
                //byte[] asciiBytes;

                System.Diagnostics.Debug.WriteLine("Logic: Read {0} bytes, starting with 0x{1:X}", data.Length, (ushort)data[0]);                
                if (analyzeI2C == true)
                {
                    int change = 0;
                    for (i = 0; i < data.Length; i++)
                    {
                        levels = data[i];
                        changeLevels = (byte)(lastLevels ^ levels);
                        if ((changeLevels & maskSDA) != 0)
                        {
                            // SDA changed since last report
                            change++;
                            if ((levels & maskSCL) != 0)
                            {
                                if (ignoreGlitch == true)
                                {
                                    ignoreGlitch = false;
                                }
                                else
                                {
                                    // SDA changed while SCL is HIGH, START or STOP
                                    if (i2cState == I2C_STATE_READING)
                                    {
                                        // SDA changed from LOW to HIGH, STOP condition
                                        //System.Diagnostics.Debug.Write("SDA changed from LOW to HIGH, STOP condition\r\n");
                                        i2cState = I2C_STATE_IDLE;
                                        file.Write("]\r\n");
                                        System.Diagnostics.Debug.Write("]\r\n");

                                    }
                                    else
                                    {
                                        // SDA changed from HIGH to LOW, START condition
                                        //System.Diagnostics.Debug.Write("SDA changed from HIGH to LOW, START condition\r\n");
                                        i2cState = I2C_STATE_READING;
                                        file.Write("[");
                                        System.Diagnostics.Debug.Write("[");

                                        currentBitPos = 7; // I2C protocol is big-endian
                                        currentByte = 0;
                                    }
                                }
                            }
                        }
                        if ((changeLevels & maskSCL) != 0)
                        {
                            if ((ignoreGlitch == false) && (i2cState != I2C_STATE_IDLE))
                            {
                                // SCL changed since last report
                                change++;
                                if ((levels & maskSCL) != 0)
                                {
                                    // SCL is HIGH, start of pulsed bit read or acknowledge
                                    if (currentBitPos < 8)
                                    {
                                        // 0-7 = data bit
                                        if ((levels & maskSDA) != 0)
                                        {
                                            // SDA is HIGH, bit=1 or ACK
                                            //System.Diagnostics.Debug.Write("1");
                                            currentByte |= (byte)(1 << currentBitPos);
                                        }
                                        else
                                        {
                                            // SDA is LOW, bit=0 or NACK
                                            //System.Diagnostics.Debug.Write("0");
                                        }
                                        //if (currentBitPos == 0 && i2cState == I2C_STATE_PREBYTE)
                                        //{
                                        //    i2cState = I2C_STATE_POSTBYTE;
                                        //}
                                        currentBitPos--; // will wrap around to 255 for 9th (ack/nack) bit
                                    }
                                    else
                                    {
                                        // 255 = ack/nack bit
                                        if ((levels & maskSDA) != 0)
                                        {
                                            // SDA is HIGH, bit=1 or NACK
                                            //System.Diagnostics.Debug.Write("N");
                                        }
                                        else
                                        {
                                            // SDA is LOW, bit=0 or ACK
                                            //System.Diagnostics.Debug.Write("A\r\n" + currentByte.ToString("X") + "\r\n");  
                                            file.Write(currentByte.ToString("X2") + " ");
                                            System.Diagnostics.Debug.Write(currentByte.ToString("X2") + " ");                                            
                                        }

                                        /*if (i2cState == I2C_STATE_READING)
                                        {
                                            //printf("%02X%c ", currentByte, currentAck ? '-' : '+');
                                            System.Diagnostics.Debug.Write(currentByte.ToString());
                                            packetData[packetBytePos] = currentByte;
                                            packetAck[packetBytePos] = currentAck;
                                            packetBytePos++;
                                            //i2cState = I2C_STATE_PREBYTE;
                                        }*/

                                        currentBitPos = 7;
                                        currentByte = 0;
                                    }
                                }
                            }
                        }
                        lastLevels = levels;
                    }
                }
                else
                {
                    for (i = 0; i < data.Length; i++)
                    {
                        if ((sampleNumber == 0) || ((data[i] & bitMask) != previousSample))
                        {
                            previousSample = (byte)(data[i] & bitMask);
                            file.Write(sampleNumber.ToString());
                            if ((bitMask & 0x80) != 0) file.Write("," + ((char)(((data[i] & 0x80) >> 7) + '0')).ToString());
                            if ((bitMask & 0x40) != 0) file.Write("," + ((char)(((data[i] & 0x40) >> 6) + '0')).ToString());
                            if ((bitMask & 0x20) != 0) file.Write("," + ((char)(((data[i] & 0x20) >> 5) + '0')).ToString());
                            if ((bitMask & 0x10) != 0) file.Write("," + ((char)(((data[i] & 0x10) >> 4) + '0')).ToString());
                            if ((bitMask & 0x08) != 0) file.Write("," + ((char)(((data[i] & 0x08) >> 3) + '0')).ToString());
                            if ((bitMask & 0x04) != 0) file.Write("," + ((char)(((data[i] & 0x04) >> 2) + '0')).ToString());
                            if ((bitMask & 0x02) != 0) file.Write("," + ((char)(((data[i] & 0x02) >> 1) + '0')).ToString());
                            if ((bitMask & 0x01) != 0) file.Write("," + ((char)(((data[i] & 0x01)) + '0')).ToString());
                            file.WriteLine(String.Empty);
                            //writeStr = sampleNumber.ToString() + "," + ((char)((data[i] & 0x01) + '0')).ToString();
                            //asciiBytes = Encoding.ASCII.GetBytes(writeStr);
                            //accessor.WriteArray(accessorOffset, asciiBytes, 0, asciiBytes.Length);
                            //accessorOffset += asciiBytes.Length;
                            //accessor.Write(accessorOffset++, '\n');
                        }

                        sampleNumber++;
                    }
                }
            }
            catch (Exception)
            {
                System.Diagnostics.Debug.WriteLine("Logic Analyzer is attempting to write to a closed file");
            }
        }


        void mLogic16_OnReadData(ulong device_id, byte[] dataBytes)
        {
            try
            {
                int i;
                ushort levels;
                int maskSDA = 0x80;
                int maskSCL = 0x40;
                //string writeStr;
                //byte[] asciiBytes;

                // the 16-bit logic analyzer sends its data in 16-bit words. in the byte stream "dataBytes" these words are separated into two bytes
                // Here we are taking the byte stream and creating the 16-bit words of logic data
                ushort[] data = new ushort[dataBytes.Length / 2];
                for (int k = 0; k < (dataBytes.Length / 2); k++)
                    data[k] = (ushort)((dataBytes[(k * 2)+1]<<8) + dataBytes[k*2]);

                    System.Diagnostics.Debug.WriteLine("Logic16: Read {0} bytes, starting with 0x{1:X}", data.Length, data[0]);
                //System.Diagnostics.Debug.WriteLine(data[0].ToString() + " " + data[1].ToString() + " " + data[2].ToString() + " " + data[3].ToString());

                if (analyzeI2C == true)
                {
                    int change = 0;
                    for (i = 0; i < data.Length; i++)
                    {
                        levels = data[i];
                        changeLevels16 = (byte)(lastLevels16 ^ levels);
                        if ((changeLevels16 & maskSDA) != 0)
                        {
                            // SDA changed since last report
                            change++;
                            if ((levels & maskSCL) != 0)
                            {
                                if (ignoreGlitch == true)
                                {
                                    ignoreGlitch = false;
                                }
                                else
                                {
                                    // SDA changed while SCL is HIGH, START or STOP
                                    if (i2cState == I2C_STATE_READING)
                                    {
                                        // SDA changed from LOW to HIGH, STOP condition
                                        //System.Diagnostics.Debug.Write("SDA changed from LOW to HIGH, STOP condition\r\n");
                                        i2cState = I2C_STATE_IDLE;
                                        file.Write("]\r\n");
                                        System.Diagnostics.Debug.Write("]\r\n");

                                    }
                                    else
                                    {
                                        // SDA changed from HIGH to LOW, START condition
                                        //System.Diagnostics.Debug.Write("SDA changed from HIGH to LOW, START condition\r\n");
                                        i2cState = I2C_STATE_READING;
                                        file.Write("[");
                                        System.Diagnostics.Debug.Write("[");

                                        currentBitPos = 7; // I2C protocol is big-endian
                                        currentByte = 0;
                                    }
                                }
                            }
                        }
                        if ((changeLevels16 & maskSCL) != 0)
                        {
                            if ((ignoreGlitch == false) && (i2cState != I2C_STATE_IDLE))
                            {
                                // SCL changed since last report
                                change++;
                                if ((levels & maskSCL) != 0)
                                {
                                    // SCL is HIGH, start of pulsed bit read or acknowledge
                                    if (currentBitPos < 8)
                                    {
                                        // 0-7 = data bit
                                        if ((levels & maskSDA) != 0)
                                        {
                                            // SDA is HIGH, bit=1 or ACK
                                            //System.Diagnostics.Debug.Write("1");
                                            currentByte |= (byte)(1 << currentBitPos);
                                        }
                                        else
                                        {
                                            // SDA is LOW, bit=0 or NACK
                                            //System.Diagnostics.Debug.Write("0");
                                        }
                                        //if (currentBitPos == 0 && i2cState == I2C_STATE_PREBYTE)
                                        //{
                                        //    i2cState = I2C_STATE_POSTBYTE;
                                        //}
                                        currentBitPos--; // will wrap around to 255 for 9th (ack/nack) bit
                                    }
                                    else
                                    {
                                        // 255 = ack/nack bit
                                        if ((levels & maskSDA) != 0)
                                        {
                                            // SDA is HIGH, bit=1 or NACK
                                            //System.Diagnostics.Debug.Write("N");
                                        }
                                        else
                                        {
                                            // SDA is LOW, bit=0 or ACK
                                            //System.Diagnostics.Debug.Write("A\r\n" + currentByte.ToString("X") + "\r\n");  
                                            file.Write(currentByte.ToString("X2") + " ");
                                            System.Diagnostics.Debug.Write(currentByte.ToString("X2") + " ");
                                        }



                                        currentBitPos = 7;
                                        currentByte = 0;
                                    }
                                }
                            }
                        }
                        lastLevels16 = levels;
                    }
                }
                else
                {
                    for (i = 0; i < data.Length; i++)
                    {
                        if ((sampleNumber == 0) || ((data[i] & bitMask) != previousSample))
                        {
                            previousSample = (byte)(data[i] & bitMask);
                            file.Write(sampleNumber.ToString());
                            if ((bitMask & 0x80) != 0) file.Write("," + ((char)(((data[i] & 0x80) >> 7) + '0')).ToString());
                            if ((bitMask & 0x40) != 0) file.Write("," + ((char)(((data[i] & 0x40) >> 6) + '0')).ToString());
                            if ((bitMask & 0x20) != 0) file.Write("," + ((char)(((data[i] & 0x20) >> 5) + '0')).ToString());
                            if ((bitMask & 0x10) != 0) file.Write("," + ((char)(((data[i] & 0x10) >> 4) + '0')).ToString());
                            if ((bitMask & 0x08) != 0) file.Write("," + ((char)(((data[i] & 0x08) >> 3) + '0')).ToString());
                            if ((bitMask & 0x04) != 0) file.Write("," + ((char)(((data[i] & 0x04) >> 2) + '0')).ToString());
                            if ((bitMask & 0x02) != 0) file.Write("," + ((char)(((data[i] & 0x02) >> 1) + '0')).ToString());
                            if ((bitMask & 0x01) != 0) file.Write("," + ((char)(((data[i] & 0x01)) + '0')).ToString());
                            file.WriteLine(String.Empty);
                            //writeStr = sampleNumber.ToString() + "," + ((char)((data[i] & 0x01) + '0')).ToString();
                            //asciiBytes = Encoding.ASCII.GetBytes(writeStr);
                            //accessor.WriteArray(accessorOffset, asciiBytes, 0, asciiBytes.Length);
                            //accessorOffset += asciiBytes.Length;
                            //accessor.Write(accessorOffset++, '\n');
                        }

                        sampleNumber++;
                    }
                }
            }
            catch (Exception)
            {
                System.Diagnostics.Debug.WriteLine("Logic Analyzer is attempting to write to a closed file");
            }
        }



        void mLogic_OnWriteData(ulong device_id, byte[] data)
        {

        }

        void mLogic_OnError(ulong device_id)
        {
            System.Diagnostics.Debug.WriteLine("Logic Reported an Error.  This probably means that Logic could not keep up at the given data rate, or was disconnected. You can re-start the capture automatically, if your application can tolerate gaps in the data.");            
        }
    }

}
