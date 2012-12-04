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
    public class TelnetBoard
    {
        public enum CommandStatus
        {
            Done,
            Running,
            Error
        }

        private IPEndPoint ipEnd = new IPEndPoint(IPAddress.Any, 4444);    // IP address and port we are listening on
        private Thread ListenThread;
        public Socket connectSocket;
        public MainWindow mainHandle;
        private CommandStatus commandResult;
        private string expectedResponse = String.Empty;

        private static AutoResetEvent ARE_result = new AutoResetEvent(false);

        public TelnetBoard(MainWindow passedHandle)
        {

            mainHandle = passedHandle;            
        }

        public bool Start()
        {
            try
            {
                IPAddress[] ipAddress = Dns.GetHostAddresses("127.0.0.1");
                IPEndPoint ipEnd = new IPEndPoint(ipAddress[0], 4444);
                connectSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
                System.Diagnostics.Debug.WriteLine("Connecting to board through telnet.");
                connectSocket.Connect(ipEnd);

                if (connectSocket != null)
                {
                    // starting thread to receive data over TCP/IP
                    ListenThread = new Thread(new ThreadStart(ListenThreadFunction));
                    ListenThread.Start();
                    ListenThread.Name = "Receive Socket Listen Thread";
                }
                else 
                    return false;

                return true;

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Board telnet start:" + ex.ToString());
                return false;
            }
        }

        public bool Clear()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Sending commands to board: ");
                waitForMessages();

                if (RunCommand("stm32f1x mass_erase 0\r\n", "stm32x mass erase complete", 5000) != CommandStatus.Done)
                {
                    System.Diagnostics.Debug.WriteLine("Telnet failed to stm32f1x mass_erase 0.");
                    return false;
                }
                if (RunCommand("stm32f1x mass_erase 1\r\n", "stm32x mass erase complete", 5000) != CommandStatus.Done)
                {
                    System.Diagnostics.Debug.WriteLine("Telnet failed to stm32f1x mass_erase 1.");
                    return false;
                }               
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Telnet clear FAIL." + ex.ToString());
                return false;
            }
        }

        public bool Load(string s19File)
        {
            try
            {
                string modifiedS19File = s19File.Replace(@"\", @"\\");
                System.Diagnostics.Debug.WriteLine("Sending commands to board: ");
                waitForMessages();                
                
                System.Diagnostics.Debug.WriteLine("flash write_image: ");
                if (RunCommand("flash write_image " + modifiedS19File + "\r\n", "wrote", 150000) != CommandStatus.Done)
                {
                    System.Diagnostics.Debug.WriteLine("Telnet failed to write flash image.");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Telnet load FAIL." + ex.ToString());
                return false;
            }
        }

        public void Kill()
        {
            try
            {
                RunCommand("exit", "halted", 100);
                System.Diagnostics.Debug.WriteLine("Telnet board kill listen thread.");

                connectSocket.Close();

                if (ListenThread != null)
                {
                    // socket must be forced closed before listen thread can be killed
                    if (ListenThread.IsAlive) ListenThread.Abort();
                    ListenThread.Join(100);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("TelnetInfoProcess already killed. Can't kill again: " + ex.ToString());
            }
        }

        private CommandStatus RunCommand(string command, string expect, int timeout)
        {
            int attempts;
            expectedResponse = expect;

            for (attempts = 0; attempts < 3; attempts++)
            {
                System.Diagnostics.Debug.WriteLine("\r\nTelnet run attempt " + attempts.ToString() + " for: " + command + " waiting for: " + expect.ToString());
                commandResult = CommandStatus.Running;
                Send(connectSocket, Encoding.UTF8.GetBytes(command), 0, command.Length, 10000);
                ARE_result.WaitOne(timeout);
                if (commandResult == CommandStatus.Done)
                    break;
            }
            System.Diagnostics.Debug.WriteLine("\r\nTelnet: waiting for messages.");            
            waitForMessages();
            System.Diagnostics.Debug.WriteLine("\r\nTelnet done waiting: " + command + " complete");
            return commandResult;
        }

        private void Send(Socket socket, byte[] buffer, int offset, int size, int timeout)
        {
            int startTickCount = Environment.TickCount;
            int sent = 0;  // how many bytes is already sent
            do
            {
                if (Environment.TickCount > startTickCount + timeout)
                    throw new Exception("Timeout.");
                try
                {
                    sent += socket.Send(buffer, offset + sent, size - sent, SocketFlags.None);
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.WouldBlock ||
                        ex.SocketErrorCode == SocketError.IOPending ||
                        ex.SocketErrorCode == SocketError.NoBufferSpaceAvailable)
                    {
                        // socket buffer is probably full, wait and try again
                        Thread.Sleep(30);
                    }
                    else
                        throw ex;  // any serious error occurr
                }
            } while (sent < size);
        }

        private void ListenThreadFunction()
        {
            byte[] clientData = new byte[1024 * 5000];
            while (true)
            {
                try
                {
                    System.Diagnostics.Debug.Write(">>>>>>>>>>>>>>>>>>>>>>>> telnet from board: " + connectSocket.RemoteEndPoint.ToString() + " : ");
                    int receivedBytesLen = connectSocket.Receive(clientData);

                    processResponse(clientData);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Board listen thread FAIL: " + ex.Message);
                }
            }
        }

        private void processResponse(byte[] clientData)
        {
            System.Text.Encoding encoding = new System.Text.ASCIIEncoding();
            String rxString = encoding.GetString(clientData);

            try
            {
                System.Diagnostics.Debug.WriteLine(rxString.ToString());
                if (rxString.Contains(expectedResponse))
                {
                    commandResult = CommandStatus.Done;
                    System.Diagnostics.Debug.WriteLine("\r\nTelnet: matched response: " + expectedResponse);
                    ARE_result.Set();
                }
                    
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("process Response: " + ex.Message);
            }
        }

        private bool waitForMessages()
        {
            expectedResponse = "This string should never be matched in the telnet output";
            ARE_result.WaitOne(500);
            return true;
        }
    }
}
