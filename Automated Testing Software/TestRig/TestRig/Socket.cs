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
                    // this thread will continue to listen for and accept TCP/IP connections and will parse any test configuration files
                    connectSocket = Rxsocket.Accept();
                    System.Diagnostics.Debug.WriteLine("Receive socket accepted connection.");

                    byte[] clientData = new byte[1024 * 5000];

                    System.Diagnostics.Debug.WriteLine("Receiving test config file from: " + connectSocket.RemoteEndPoint.ToString());
                    int receivedBytesLen = connectSocket.Receive(clientData);

                    readXML(clientData);

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
                return "";
            s = s.Replace("\n", String.Empty);
            s = s.Replace("\t", String.Empty);
            s = s.Replace("\r", String.Empty);
            s = s.Replace("\0", String.Empty);

            return s;
        }

        public TestDescription readXMLTest(XmlReader reader)
        {
            TestDescription readTest = new TestDescription();

            reader.ReadToFollowing("Test");
            reader.MoveToFirstAttribute();
            readTest.testName = reader.Value;
            //System.Diagnostics.Debug.WriteLine("The name value: " + readTest.testName);
            reader.MoveToNextAttribute();
            readTest.testType = reader.Value;
            //System.Diagnostics.Debug.WriteLine("The type value: " + readTest.testType);

            reader.ReadToFollowing("Description");
            readTest.testDescription = strip(reader.ReadElementContentAsString());
            //System.Diagnostics.Debug.WriteLine("Description of test: " + readTest.testDescription);

            reader.ReadToFollowing("TestPath");
            readTest.testPath = strip(reader.ReadElementContentAsString());
            //System.Diagnostics.Debug.WriteLine("Test path of test: " + readTest.testPath);

            reader.ReadToFollowing("TestProjName");
            readTest.buildProj = strip(reader.ReadElementContentAsString());
            //System.Diagnostics.Debug.WriteLine("Build project name of test: " + readTest.buildProj);

            reader.ReadToFollowing("TesterName");
            readTest.testerName = strip(reader.ReadElementContentAsString());
            //System.Diagnostics.Debug.WriteLine("Tester Name: " + readTest.testerName);

            reader.ReadToFollowing("TestLocation");
            readTest.testLocation = strip(reader.ReadElementContentAsString());
            //System.Diagnostics.Debug.WriteLine("Test Location: " + readTest.testLocation);

            reader.ReadToFollowing("TestMFVersionNum");
            readTest.testMFVersionNum = strip(reader.ReadElementContentAsString());
            //System.Diagnostics.Debug.WriteLine("MF Version number: " + readTest.testMFVersionNum);

            reader.ReadToFollowing("TestGitOption");
            readTest.testGitOption = strip(reader.ReadElementContentAsString());
            //System.Diagnostics.Debug.WriteLine("Git Option: " + readTest.testGitOption);

            reader.ReadToFollowing("TestGitBranch");
            readTest.testGitBranch = strip(reader.ReadElementContentAsString());
            //System.Diagnostics.Debug.WriteLine("Git Option: " + readTest.testGitBranch);

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

                        if (readTest.testName.Length > 0)
                        {
                            // waiting for mutex to be free
                            testCollectionMutex.WaitOne();
                            // queueing up test that was just parsed out in local machine test queue
                            testCollectionSocket.Enqueue(readTest);
                            // updating test status tab display
                            mainHandle.Dispatcher.BeginInvoke(mainHandle.addDelegate, readTest);
                            testCollectionMutex.ReleaseMutex();
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("XML reading ended with exception, fix this: " + ex.Message);
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
            }
        }
    }
}
