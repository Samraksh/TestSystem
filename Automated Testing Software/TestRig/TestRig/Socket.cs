using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Collections.ObjectModel;
using System.Xml;
using System.Windows.Controls;

namespace TestRig
{
    class ReceiveSocket
    {
        private IPEndPoint ipEnd = new IPEndPoint(IPAddress.Any, 32001);    // IP address and port we are listening on
        private Socket Rxsocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
        private Thread ListenThread;
        public Socket connectSocket;
        public static Queue<TestDescription> testCollectionSocket;
        private Mutex testCollectionMutex;
        public MainWindow mainHandle;

        public ReceiveSocket(Queue<TestDescription> testCollection, Mutex collectionMutex, MainWindow passedHandle)
        {            
            // currently listening for any incoming IP address on port 32001
            Rxsocket.Bind(ipEnd);
            Rxsocket.Listen(1000);

            testCollectionSocket = testCollection;
            testCollectionMutex = collectionMutex;

            mainHandle = passedHandle;

            // starting thread to receive data over TCP/IP
            // Test config files received will be parsed and queued
            ListenThread = new Thread(new ThreadStart(ListenThreadFunction));
            ListenThread.Start();
            ListenThread.Name = "Receive Socket Listen Thread";
        }

        public void KillListenThread()
        {
            // listen thread needs to be killed when program is closed
            System.Diagnostics.Debug.WriteLine("Killing receive socket listen thread.");
            if (ListenThread != null)
            {
                // socket must be forced closed before listen thread can be killed
                if (Rxsocket != null) Rxsocket.Close();
                if (ListenThread.IsAlive) ListenThread.Abort();
                ListenThread.Join(100);
            }
        }

        private void ListenThreadFunction()
        {
            while (true)
            {
                try
                {
                    int receivedBytesLen;
                    // this thread will continue to listen for and accept TCP/IP connections and will parse any test configuration files
                    System.Diagnostics.Debug.Write("attempting to accept rx connection\r\n");
                    connectSocket = Rxsocket.Accept();
                    System.Diagnostics.Debug.WriteLine("Receive socket accepted connection.");

                    byte[] clientData = new byte[1024 * 250];

                    System.Diagnostics.Debug.WriteLine("Receiving test config file from: " + connectSocket.RemoteEndPoint.ToString());
                    receivedBytesLen = connectSocket.Receive(clientData);
                    BinaryWriter bWrite = new BinaryWriter(File.Open("receive.config", FileMode.Create));
                    while (receivedBytesLen != 0)
                    {
                        bWrite.Write(clientData, 0, receivedBytesLen);
                        receivedBytesLen = connectSocket.Receive(clientData);
                    }
                    Console.WriteLine("File: {0} received over RX socket and saved", "receive.config");
                    bWrite.Flush();
                    bWrite.Close();

                    byte[] fileData = File.ReadAllBytes("receive.config");
                    readXML(fileData);

                    connectSocket.Close();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Listen thread FAIL: " + ex.Message);
                }
            }
        }

        private string strip(string s)
        {
            if (s == null)
                return String.Empty;
            s = s.Replace("\n", String.Empty);
            s = s.Replace("\t", String.Empty);
            s = s.Replace("\r", String.Empty);
            s = s.Replace("\0", String.Empty);

            return s;
        }

        public TestDescription readXMLTest(XmlReader reader)
        {
            TestDescription readTest = new TestDescription();

            if (reader.ReadToFollowing("Test") == false) return readTest;
            if (reader.MoveToFirstAttribute() == false) return readTest;
            readTest.testName = reader.Value;
            //System.Diagnostics.Debug.WriteLine("The name value: " + readTest.testName);
            if (reader.MoveToNextAttribute() == false) return readTest;
            readTest.testType = reader.Value;
            //System.Diagnostics.Debug.WriteLine("The type value: " + readTest.testType);

            if (reader.ReadToFollowing("Description") == false) return readTest;
            readTest.testDescription = strip(reader.ReadElementContentAsString());
            //System.Diagnostics.Debug.WriteLine("Description of test: " + readTest.testDescription);

            if (reader.ReadToFollowing("TestPath") == false) return readTest;
            readTest.testPath = strip(reader.ReadElementContentAsString());
            //System.Diagnostics.Debug.WriteLine("Test path of test: " + readTest.testPath);

            if (reader.ReadToFollowing("TestProjName") == false) return readTest;
            readTest.buildProj = strip(reader.ReadElementContentAsString());
            //System.Diagnostics.Debug.WriteLine("Build project name of test: " + readTest.buildProj);

            if (reader.ReadToFollowing("TesterName") == false) return readTest;
            readTest.testerName = strip(reader.ReadElementContentAsString());
            //System.Diagnostics.Debug.WriteLine("Tester Name: " + readTest.testerName);

            if (reader.ReadToFollowing("TestLocation") == false) return readTest;
            readTest.testLocation = strip(reader.ReadElementContentAsString());
            //System.Diagnostics.Debug.WriteLine("Test Location: " + readTest.testLocation);

            if (reader.ReadToFollowing("TestMFVersionNum") == false) return readTest;
            readTest.testMFVersionNum = strip(reader.ReadElementContentAsString());
            //System.Diagnostics.Debug.WriteLine("MF Version number: " + readTest.testMFVersionNum);

            if (reader.ReadToFollowing("TestGitOption") == false) return readTest;
            readTest.testGitOption = strip(reader.ReadElementContentAsString());
            //System.Diagnostics.Debug.WriteLine("Git Option: " + readTest.testGitOption);

            if (reader.ReadToFollowing("TestGitBranch") == false) return readTest;
            readTest.testGitBranch = strip(reader.ReadElementContentAsString());
            //System.Diagnostics.Debug.WriteLine("Git Option: " + readTest.testGitBranch);

            if (reader.ReadToFollowing("TestUsePrecompiledBinary") == true)
                readTest.testUsePrecompiledBinary = strip(reader.ReadElementContentAsString());
            else
                readTest.testUsePrecompiledBinary = String.Empty;

            if (reader.ReadToFollowing("TestHardware") == true)
                readTest.testHardware = strip(reader.ReadElementContentAsString());
            else
                readTest.testHardware = String.Empty;
            if (reader.ReadToFollowing("TestSolution") == true)
                readTest.testSolution = strip(reader.ReadElementContentAsString());
            else
                readTest.testSolution = String.Empty;
            if (reader.ReadToFollowing("TestMemoryType") == true)
                readTest.testMemoryType = strip(reader.ReadElementContentAsString());
            else
                readTest.testMemoryType = String.Empty;
            if (reader.ReadToFollowing("TestSolutionType") == true)
                readTest.testSolutionType = strip(reader.ReadElementContentAsString());
            else
                readTest.testSolutionType = String.Empty;
            if (reader.ReadToFollowing("TestGCCVersion") == true)
                readTest.testGCCVersion = strip(reader.ReadElementContentAsString());
            else
                readTest.testGCCVersion = String.Empty;
            if (reader.ReadToFollowing("TestSupporting") == true)
                readTest.testSupporting = strip(reader.ReadElementContentAsString());
            else
                readTest.testSupporting = String.Empty;

            readTest.testReadComplete = true;
            return readTest;
        }

        private void readXML(byte[] clientData)
        {
            // parsing a received test configuration file. the tests will be added to the test queue.
            System.Text.Encoding encoding = new System.Text.ASCIIEncoding();
            String XMLString = encoding.GetString(clientData);

            // Create an XmlReader
            using (XmlReader reader = XmlReader.Create(new StringReader(XMLString)))
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("Starting to parse received XML test file.");
                    while (reader != null)
                    {                        
                        TestDescription readTest = readXMLTest(reader);

                        if (readTest.testReadComplete == true)
                        {
                            // waiting for mutex to be free
                            testCollectionMutex.WaitOne();
                            // queueing up test that was just parsed out in local machine test queue
                            System.Diagnostics.Debug.WriteLine("Queue test: " + readTest.testName);
                            testCollectionSocket.Enqueue(readTest);
                            // updating test status tab display
                            mainHandle.Dispatcher.BeginInvoke(mainHandle.addDelegate, readTest);
                            testCollectionMutex.ReleaseMutex();
                        }
                        else
                            return;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("XML reading ended: " + ex.Message);
                }
            }
        }

        public void uploadTests()
        {
            try
            {
                // sending the generated test configuration file to the test machine
                System.Diagnostics.Debug.WriteLine("Starting Program.");
                IPAddress[] ipAddress = Dns.GetHostAddresses("127.0.0.1");
                IPEndPoint ipEnd = new IPEndPoint(ipAddress[0], 32001);
                Socket clientSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
                System.Diagnostics.Debug.WriteLine("Connecting.");
                clientSock.Connect(ipEnd);

                string filePath = "test.config";    //test config file path
                byte[] fileData = File.ReadAllBytes(filePath);
                byte[] clientData = new byte[fileData.Length];
                
                fileData.CopyTo(clientData, 0);
                clientSock.Send(clientData);
                clientSock.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Test config file sending FAIL." + ex.ToString());
                MainWindow.showMessageBox("Failed to connect to machine at IP: " + ipEnd.ToString());
            }
        }
    }
}
