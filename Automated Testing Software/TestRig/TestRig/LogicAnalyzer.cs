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
    class i2cPacket {
		public int devAddr;
        public int regAddr;
		public bool read;
        //public int* data;
		//public bool *ack;
        public int length;
        public int error;

		/*void print() {
			if (length > 0) {
				printf("0x%02X @ 0x%02X [%3d]: %s ", regAddr, devAddr, length, read ? "==>" : "<==");
				for (U32 i = 0; i < length; i++) printf(" %02X", data[i]);
			} else {
				printf("[?]");
			}
			if (error > 0) printf(" ERR: %d", error);
			printf("\n");
		}*/
};

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
        private int lastLevels;
        private int changeLevels;
        private const int I2C_STATE_IDLE =	0;
        private const int I2C_STATE_PREBYTE	 = 1;
        private const int I2C_STATE_POSTBYTE =2;
        private int i2cState = I2C_STATE_IDLE;
        private int packetBytePos = 0;
private int[] packetData = new int[1024];
bool[] packetAck = new bool[1024];
private int currentBitPos;
private int currentByte;
bool currentAck;
private i2cPacket packet = new i2cPacket();
bool readSetup = false;

        public LogicAnalyzer(int sampleFrequency, string projectName)
        {
            mSampleRateHz = (UInt32)sampleFrequency;
            devices = new MSaleaeDevices();
            devices.OnLogicConnect += new MSaleaeDevices.OnLogicConnectDelegate(devices_LogicOnConnect);
            devices.OnLogic16Connect += new MSaleaeDevices.OnLogic16ConnectDelegate(devices_Logic16OnConnect);
            devices.OnDisconnect += new MSaleaeDevices.OnDisconnectDelegate(devices_OnDisconnect);
            devices.BeginConnect();

            analyzeI2C = false;
            lastLevels = 0;
            changeLevels = 0;            

            setBitMask(projectName);

            // wait for device to be found; sleep for 2 second
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
                int i,levels;
                int maskSDA = 0x4;
                int maskSCL = 0x2;
                //string writeStr;
                //byte[] asciiBytes;

                System.Diagnostics.Debug.WriteLine("Logic: Read {0} bytes, starting with 0x{1:X}", data.Length, (ushort)data[0]);

                if (analyzeI2C == true)
                {
                   /* int change = 0;
                    for (i = 0; i < data.Length; i++)
                    {
                        levels = data[i];
                        changeLevels = lastLevels ^ levels;
                        if ( (changeLevels & maskSDA) != 0)
                        {
                            // SDA changed since last report
                            change++;
                            if ((levels & maskSCL) != 0)
                            {
                                // SDA changed while SCL is HIGH, START or STOP
                                if ((changeLevels & maskSDA) != 0)
                                {
                                    // SDA changed from LOW to HIGH, STOP condition
                                    if (i2cState == I2C_STATE_PREBYTE)
                                    {
                                        // STOP condition found while busy, good
                                        i2cState = I2C_STATE_IDLE;
                                        //printf("]\n");
                                    }
                                    else
                                    {
                                        // ERROR: STOP condition found while already idle
                                        //printf("] (error, state=%d)\n", i2cState);
                                    }
                                    if (packetBytePos == 2 && ((packetData[0] & 1) == 0))
                                    {
                                        // setup read packet, just a dev addr + reg addr in this one
                                        packet.devAddr = packetData[0] >> 1;
                                        packet.regAddr = packetData[1];
                                        packet.read = true;
                                        readSetup = true;
                                    }
                                    else if (packetBytePos > 0)
                                    {
                                        if (readSetup)
                                        {
                                            // result of previous read request
                                            if (packet data != null) free(packet.data);
                                            packet.data = (int*)malloc(packetBytePos - 1);
                                            packet.length = packetBytePos - 1;
                                            memcpy(packet.data, packetData + 1, packet.length);
                                            packet.print();
                                            readSetup = false;
                                        }
                                        else if (packetBytePos > 2)
                                        {
                                            // regular write packet
                                            packet.devAddr = packetData[0] >> 1;
                                            packet.read = false;
                                            packet.regAddr = packetData[1];
                                            if (packet.data != NULL) free(packet.data);
                                            packet.data = (U8*)malloc(packetBytePos - 2);
                                            packet.length = packetBytePos - 2;
                                            memcpy(packet.data, packetData + 2, packet.length);
                                            packet.print();
                                        }
                                    }
                                    packetBytePos = 0;
                                }
                                else
                                {
                                    // SDA changed from HIGH to LOW, START condition

                                    // SDA changed from HIGH to LOW, START condition
                                    if (i2cState == I2C_STATE_IDLE)
                                    {
                                        // START condition found while idle
                                        i2cState = I2C_STATE_PREBYTE;
                                        //printf("[ ");
                                    }
                                    else if (i2cState == I2C_STATE_PREBYTE)
                                    {
                                        // repeated START condition, end of last packet
                                        if (packetBytePos == 2 && ((packetData[0] & 1) == 0))
                                        {
                                            // setup read packet, just a dev addr + reg addr in this one
                                            packet.devAddr = packetData[0] >> 1;
                                            packet.regAddr = packetData[1];
                                            packet.read = true;
                                            readSetup = true;
                                        }
                                        else if (packetBytePos > 0)
                                        {
                                            if (readSetup)
                                            {
                                                // result of previous read request
                                                if (packet.data != NULL) free(packet.data);
                                                packet.data = (U8*)malloc(packetBytePos - 1);
                                                packet.length = packetBytePos - 1;
                                                memcpy(packet.data, packetData + 1, packet.length);
                                                packet.print();
                                                readSetup = false;
                                            }
                                            else if (packetBytePos > 2)
                                            {
                                                // regular write packet
                                                packet.devAddr = packetData[0] >> 1;
                                                packet.read = false;
                                                packet.regAddr = packetData[1];
                                                if (packet.data != NULL) free(packet.data);
                                                packet.data = (U8*)malloc(packetBytePos - 2);
                                                packet.length = packetBytePos - 2;
                                                memcpy(packet.data, packetData + 2, packet.length);
                                                packet.print();
                                            }
                                        }
                                        packetBytePos = 0;
                                        //printf("{ ");
                                    }
                                    else
                                    {
                                        // ERROR: START condition found while expecting something else
                                        //printf("[ (error, state=%d)\n", i2cState);
                                    }
                                    packetBytePos = 0;
                                    currentBitPos = 7; // I2C protocol is big-endian
                                    currentByte = 0;
                                    currentAck = false;
                                }
                            }
                            else
                            {
                                // SDA changed while SCL is LOW, nothing important
                            }
                        }
                        if ((changeLevels & maskSCL) != 0)
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
                                        //std::cout << "1";
                                        currentByte |= (1 << currentBitPos);
                                    }
                                    else
                                    {
                                        // SDA is LOW, bit=0 or NACK
                                        //std::cout << "0";
                                    }
                                    if (currentBitPos == 0 && i2cState == I2C_STATE_PREBYTE)
                                    {
                                        i2cState = I2C_STATE_POSTBYTE;
                                    }
                                    currentBitPos--; // will wrap around to 255 for 9th (ack/nack) bit
                                }
                                else
                                {
                                    // 255 = ack/nack bit
                                    if ((levels & maskSDA) != 0)
                                    {
                                        // SDA is HIGH, bit=1 or NACK
                                        //std::cout << "N ";
                                        currentAck = true;
                                    }
                                    else
                                    {
                                        // SDA is LOW, bit=0 or ACK
                                        //std::cout << "A ";
                                    }

                                    if (i2cState == I2C_STATE_POSTBYTE)
                                    {
                                        //printf("%02X%c ", currentByte, currentAck ? '-' : '+');
                                        packetData[packetBytePos] = currentByte;
                                        packetAck[packetBytePos] = currentAck;
                                        packetBytePos++;
                                        i2cState = I2C_STATE_PREBYTE;
                                    }

                                    currentBitPos = 7;
                                    currentByte = 0;
                                    currentAck = false;
                                }
                            }
                            else
                            {
                                // SCL is LOW, end of pulsed bit read or acknoweldge
                            }
                        }

                        lastLevels = levels;
                    }*/
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
