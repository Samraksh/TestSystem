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
        
        SerialPort serialPort;
        public MainWindow mainHandle;
        public static TestReceipt testResults;
        private static string accumReceiveString;

        public COMPort(MainWindow passedHandle, TestDescription currentTest, TestReceipt results)
        {

            mainHandle = passedHandle;
            testResults = results;
            accumReceiveString = String.Empty;

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

                serialPort.DataReceived += new SerialDataReceivedEventHandler(SerialPortHandler);

                System.Diagnostics.Debug.WriteLine("Opening COM port: " + serialPort.ToString());

                serialPort.Open();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("COM port open failed:" + ex.ToString());
                return;
            }
        }

        static void SerialPortHandler(object sender, SerialDataReceivedEventArgs e)
        {            
            SerialPort serialPort = (SerialPort)sender;
            string inData = serialPort.ReadExisting();
            System.Diagnostics.Debug.WriteLine(inData);
            accumReceiveString = String.Concat(accumReceiveString, inData);

            string strippedReceive = String.Empty;
            while ((accumReceiveString.Contains("\n")) || (accumReceiveString.Contains("\r")))
            {
                strippedReceive = accumReceiveString.Substring(0, accumReceiveString.IndexOf('\n'));
                accumReceiveString = accumReceiveString.Remove(0, accumReceiveString.IndexOf('\n') + 1);
                processResponse(strippedReceive);
            }
        }      

        public void Kill()
        {
            try
            {               
                System.Diagnostics.Debug.WriteLine("COM port kill.");

                serialPort.Close();
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
                System.Diagnostics.Debug.WriteLine("COM processing: " + rxString.ToString());
                if (rxString.Contains("result ="))
                {
                    if (rxString.Contains("PASS"))
                        testResults.testPass = true;
                    else
                        testResults.testPass = false;
                }
                else if (rxString.Contains("accuracy"))
                {
                    index = rxString.IndexOf('=') + 2;
                    rxString = rxString.TrimEnd('\r');
                    rxString = rxString.TrimEnd('\n');
                    testResults.testAccuracy = double.Parse(rxString.Substring(index, rxString.Length - index));
                }
                else if (rxString.Contains("resultParameter1"))
                {
                    index = rxString.IndexOf('=') + 2;
                    rxString = rxString.TrimEnd('\r');
                    rxString = rxString.TrimEnd('\n');
                    testResults.testReturnParameter1 = rxString.Substring(index, rxString.Length - index);
                }
                else if (rxString.Contains("resultParameter2"))
                {
                    index = rxString.IndexOf('=') + 2;
                    rxString = rxString.TrimEnd('\r');
                    rxString = rxString.TrimEnd('\n');
                    testResults.testReturnParameter2 = rxString.Substring(index, rxString.Length - index);
                }
                else if (rxString.Contains("resultParameter3"))
                {
                    index = rxString.IndexOf('=') + 2;
                    rxString = rxString.TrimEnd('\r');
                    rxString = rxString.TrimEnd('\n');
                    testResults.testReturnParameter3 = rxString.Substring(index, rxString.Length - index);
                }
                else if (rxString.Contains("resultParameter4"))
                {
                    index = rxString.IndexOf('=') + 2;
                    rxString = rxString.TrimEnd('\r');
                    rxString = rxString.TrimEnd('\n');
                    testResults.testReturnParameter4 = rxString.Substring(index, rxString.Length - index);
                }
                else if (rxString.Contains("resultParameter5"))
                {
                    index = rxString.IndexOf('=') + 2;
                    rxString = rxString.TrimEnd('\r');
                    rxString = rxString.TrimEnd('\n');
                    testResults.testReturnParameter5 = rxString.Substring(index, rxString.Length - index);
                    testResults.testComplete = true;
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
