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

        public COMPort(MainWindow passedHandle, TestDescription currentTest, TestReceipt results)
        {

            mainHandle = passedHandle;
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
                    serialPort = new SerialPort(mainHandle.textCOMPort);
                }                
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

                System.Diagnostics.Debug.WriteLine("Opening COM port: " + serialPort.ToString());

                serialPort.Open();

                // starting thread to receive data over TCP/IP
                // Test config files received will be parsed and queued
                RxThread = new Thread(new ThreadStart(ListenThreadFunction));
                RxThread.Start();
                RxThread.Name = "Receive COM Thread";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("COM port open failed:" + ex.ToString());
                return;
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
                        while ((accumReceiveString.Contains("\n")) || (accumReceiveString.Contains("\r")))
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
                    System.Diagnostics.Debug.WriteLine("COM processing: " + rxString.ToString());
                    if (rxString.Contains("result =") || rxString.Contains("result="))
                    {
                        if (rxString.Contains("PASS"))
                            testResults.testPass = true;
                        else
                            testResults.testPass = false;
                    }
                    else if (rxString.Contains("accuracy"))
                    {
                        index = rxString.IndexOf('=') + 1;
                        testResults.testAccuracy = double.Parse(rxString.Substring(index, rxString.Length - index));
                    }
                    else if (rxString.Contains("resultParameter1"))
                    {
                        index = rxString.IndexOf('=') + 1;
                        testResults.testReturnParameter1 = rxString.Substring(index, rxString.Length - index);
                    }
                    else if (rxString.Contains("resultParameter2"))
                    {
                        index = rxString.IndexOf('=') + 1;
                        testResults.testReturnParameter2 = rxString.Substring(index, rxString.Length - index);
                    }
                    else if (rxString.Contains("resultParameter3"))
                    {
                        index = rxString.IndexOf('=') + 1;
                        testResults.testReturnParameter3 = rxString.Substring(index, rxString.Length - index);
                    }
                    else if (rxString.Contains("resultParameter4"))
                    {
                        index = rxString.IndexOf('=') + 1;
                        testResults.testReturnParameter4 = rxString.Substring(index, rxString.Length - index);
                    }
                    else if (rxString.Contains("resultParameter5"))
                    {
                        index = rxString.IndexOf('=') + 1;
                        testResults.testReturnParameter5 = rxString.Substring(index, rxString.Length - index);
                        testResults.testComplete = true;
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
