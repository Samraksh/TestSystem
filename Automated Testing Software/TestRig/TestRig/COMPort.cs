using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Collections.ObjectModel;
using System.Xml;
using System.Windows.Controls;

namespace TestRig
{
    class COMPort
    {
        
        public static SerialPort serialPort;
        public MainWindow mainHandle;
        public static TestReceipt testResults;
        private Thread RxThread;
        private static string accumReceiveString;
        private static bool saveToFile;
        private static string saveFileName;
        private static System.IO.StreamWriter receiveFile;
        private static string textCOMPort;
        public bool active;
        private static bool gotResponseR = false;
        private static bool gotResponseA = false;
        private static bool gotResponse1 = false;
        private static bool gotResponse2 = false;
        private static bool gotResponse3 = false;
        private static bool gotResponse4 = false;
        private static bool gotResponse5 = false;

        public COMPort(MainWindow passedHandle)
        {
            mainHandle = passedHandle;
            active = false;
        }

        public bool Connect(TestDescription currentTest, TestReceipt results, int COMNum)
        {                        
            gotResponseR = false;
            gotResponseA = false;
            gotResponse1 = false;
            gotResponse2 = false;
            gotResponse3 = false;
            gotResponse4 = false;
            gotResponse5 = false;
                        switch (COMNum)
            {
                case (0):
                    textCOMPort = mainHandle.textCOMPortPrimary;
                    break;
                case (1):
                    textCOMPort = mainHandle.textCOMPortSecondary1;
                    break;
                default:
                    textCOMPort = mainHandle.textCOMPortPrimary;
                    break;
            }

            System.Diagnostics.Debug.WriteLine("Connecting to COM port: " + textCOMPort);

            testResults = results;
            accumReceiveString = String.Empty;
            saveToFile = false;

            try
            {
                if (currentTest.testForceCOM != String.Empty)
                {
                    serialPort = new SerialPort(currentTest.testForceCOM);
                }
                else
                {
                    serialPort = new SerialPort(textCOMPort);
                }
                System.Diagnostics.Debug.WriteLine("Opening COM port: " + textCOMPort);
                serialPort.Open();

                string[] COMParameters = currentTest.testCOMParameters.Split(',');
                serialPort.BaudRate = int.Parse(COMParameters[0]);
                switch (COMParameters[1])
                {
                    case ("N"):
                        serialPort.Parity = Parity.None;
                        break;
                    case ("E"):
                        serialPort.Parity = Parity.Even;
                        break;
                    case ("O"):
                        serialPort.Parity = Parity.Odd;
                        break;
                    case ("S"):
                        serialPort.Parity = Parity.Space;
                        break;
                    case ("M"):
                        serialPort.Parity = Parity.Mark;
                        break;
                }
                serialPort.DataBits = int.Parse(COMParameters[2]);
                switch (COMParameters[3])
                {
                    case ("N"):
                        serialPort.StopBits = StopBits.None;
                        break;
                    case ("1"):
                        serialPort.StopBits = StopBits.One;
                        break;
                    case ("2"):
                        serialPort.StopBits = StopBits.Two;
                        break;
                    case ("1.5"):
                        serialPort.StopBits = StopBits.OnePointFive;
                        break;
                }
                serialPort.Handshake = Handshake.None;

                //serialPort.DataReceived += new SerialDataReceivedEventHandler(SerialPortHandler);                

                // starting thread to receive data over TCP/IP
                // Test config files received will be parsed and queued
                RxThread = new Thread(new ThreadStart(ListenThreadFunction));
                RxThread.Start();
                RxThread.Name = "Receive COM Thread";
                active = true;
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("COM port open failed:" + ex.ToString());
                return false;
            }
        }

        private void ListenThreadFunction()
        {
            while (true)
            {
                try
                {
                    if (serialPort.BytesToRead > 0)
                    {
                        string inData = serialPort.ReadExisting();
                        System.Diagnostics.Debug.WriteLine("\r\nSerialPort Handler: ");
                        for (int i = 0; i < inData.Length; i++)
                            System.Diagnostics.Debug.Write(inData[i]);

                        if (saveToFile == true)
                        {
                            System.Diagnostics.Debug.WriteLine("Writing: " + inData + " to file.");
                            receiveFile.Write(inData);
                        }

                        accumReceiveString = String.Concat(accumReceiveString, inData);

                        string strippedReceive = String.Empty;
                        while ((accumReceiveString.Contains("\n")))
                        {
                            strippedReceive = accumReceiveString.Substring(0, accumReceiveString.IndexOf('\n') + 1);
                            accumReceiveString = accumReceiveString.Remove(0, accumReceiveString.IndexOf('\n') + 1);
                            processResponse(strippedReceive);
                        }
                    }                    
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Rx COM thread FAIL: " + ex.Message);
                }
            }
        }

        public bool Send(string SendString)
        {
            System.Diagnostics.Debug.Write("COM sending: " + SendString);
            serialPort.Write(SendString);

            return true;
        }

        public bool SendFile(string FileName)
        {
            StreamReader sr;
            string line;

            sr = new StreamReader(FileName);
            line = sr.ReadLine();
            while (line != null)
            {
                System.Diagnostics.Debug.WriteLine("COM sending: " + line);
                serialPort.WriteLine(line);
                line = sr.ReadLine();
            }

            return true;
        }

        public bool SaveToFile(string saveEnabled, string FileName)
        {
            if (saveEnabled.Equals("enable"))
            {
                if (saveToFile == false)
                {
                    saveFileName = FileName;
                    try
                    {
                        if (FileName == String.Empty) return false;
                        receiveFile = new System.IO.StreamWriter(FileName, false);

                        System.Diagnostics.Debug.WriteLine("Starting to save received COM data to file: " + FileName);
                        saveToFile = true;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("SaveToFile could not open file: " + FileName + ex.ToString());
                        return false;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Already writing to file for COM traffic.");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("No longer saving COM data to file.");
                saveToFile = false;
                if (receiveFile != null)
                {
                    receiveFile.Close();
                    receiveFile = null;
                }
            }

            return true;
        }

        public void Kill()
        {
            try
            {               
                System.Diagnostics.Debug.WriteLine("COM port kill.");
                if (RxThread != null)
                {
                    // socket must be forced closed before listen thread can be killed
                    serialPort.Close();
                    if (RxThread.IsAlive) RxThread.Abort();
                    RxThread.Join(100);
                }
                else
                {
                    serialPort.Close();
                }

                if (saveToFile == true)
                {
                    System.Diagnostics.Debug.WriteLine("No longer saving COM data to file.");
                    receiveFile.Close();
                    receiveFile = null;
                }
                active = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("COM port already killed. Can't kill again: " + ex.ToString());
            }
        }

        static private void processResponse(string rxString)
        {            
            try
            {
                int index;

                while (rxString.StartsWith("\0"))
                {
                    System.Diagnostics.Debug.Write(" *NULL* ");
                    rxString = rxString.Remove(0, 1);
                }
                rxString = rxString.TrimEnd('\r');
                rxString = rxString.TrimEnd('\n');
                rxString = rxString.TrimStart('\0');

                if (rxString != String.Empty)
                {
                    System.Diagnostics.Debug.WriteLine("COM processing (" + textCOMPort + "): " + rxString.ToString());
                    if (rxString.Contains("Exception") || rxString.Contains("exception"))
                    {
                        if (rxString.Contains("DataStoreOutOfMemoryException") && (testResults.testDescription.buildProj.Equals("Level_6A.csproj") || testResults.testDescription.buildProj.Equals("Level_6B.csproj") || testResults.testDescription.buildProj.Equals("Level_6C.csproj")))
                        {
                            //do nothing
                        }
                        //Below can be done away with after this issue is fixed - (Exception message not being suppressed #220)
                        else if ((rxString.Contains("DataStoreOutOfMemoryException") || rxString.Contains("Failure while creating data reference") || rxString.Contains("OOM exception thrown - test Level_0L successfully completed")) && testResults.testDescription.buildProj.Equals("Level_0L.csproj"))
                        {
                            testResults.testPass = true;
                            testResults.testComplete = true;
                        }
                        else
                        {
                            testResults.testPass = false;
                            testResults.testComplete = true;
                            testResults.testReturnParameter1 = "Exception thrown";
                        }
                    }
                    if (rxString.Contains("result =") || rxString.Contains("result="))
                    {
                        if (rxString.Contains("PASS"))
                            testResults.testPass = true;
                        else
                            testResults.testPass = false;
                        System.Diagnostics.Debug.WriteLine("matched test result");
                        gotResponseR = true;
                    }
                    else if (rxString.Contains("accuracy"))
                    {
                        index = rxString.IndexOf('=') + 1;
                        try
                        {
                            testResults.testAccuracy = double.Parse(rxString.Substring(index, rxString.Length - index));
                        }
                        catch (Exception ex)
                        {
                            testResults.testAccuracy = 0;
                        }
                        System.Diagnostics.Debug.WriteLine("matched accuracy");
                        gotResponseA = true;
                    }
                    else if (rxString.Contains("resultParameter1"))
                    {
                        index = rxString.IndexOf('=') + 1;
                        testResults.testReturnParameter1 = rxString.Substring(index, rxString.Length - index);
                        System.Diagnostics.Debug.WriteLine("matched resultParameter1");
                        gotResponse1 = true;
                    }
                    else if (rxString.Contains("resultParameter2"))
                    {
                        index = rxString.IndexOf('=') + 1;
                        testResults.testReturnParameter2 = rxString.Substring(index, rxString.Length - index);
                        System.Diagnostics.Debug.WriteLine("matched resultParameter2");
                        gotResponse2 = true;
                    }
                    else if (rxString.Contains("resultParameter3"))
                    {
                        index = rxString.IndexOf('=') + 1;
                        testResults.testReturnParameter3 = rxString.Substring(index, rxString.Length - index);
                        System.Diagnostics.Debug.WriteLine("matched resultParameter3");
                        gotResponse3 = true;
                    }
                    else if (rxString.Contains("resultParameter4"))
                    {
                        index = rxString.IndexOf('=') + 1;
                        testResults.testReturnParameter4 = rxString.Substring(index, rxString.Length - index);
                        System.Diagnostics.Debug.WriteLine("matched resultParameter4");
                        gotResponse4 = true;
                    }
                    else if (rxString.Contains("resultParameter5"))
                    {
                        index = rxString.IndexOf('=') + 1;
                        testResults.testReturnParameter5 = rxString.Substring(index, rxString.Length - index);
                        System.Diagnostics.Debug.WriteLine("matched resultParameter5");
                        gotResponse5 = true;
                    }
                    if ((gotResponseR == true) && (gotResponseA == true) && (gotResponse1 == true) && (gotResponse2 == true) && (gotResponse3 == true) && (gotResponse4 == true) && (gotResponse5 == true))
                    {
                        testResults.testComplete = true;
                        System.Diagnostics.Debug.WriteLine("Got all test results.");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("process Response: " + ex.Message);
                testResults.testComplete = false;
            }
        }
    }
}
