using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace TestRig
{
    public class GDB
    {
        public enum CommandStatus
        {
            Done,
            Running,
            Error
        }

        private CommandStatus commandResult;
        private string expectedPassResponse = String.Empty, expectedFailResponse = String.Empty;
        public StreamWriter input = null;

        public bool gdbConnnected = false;

        private StringWriter stdOutput = new StringWriter();
        public StringWriter Output { get { return stdOutput; } }
        private StringWriter stdError = new StringWriter();
        public StringWriter Error { get { return stdError; } }

        private static AutoResetEvent ARE_result = new AutoResetEvent(false);

        private Process GDBProcess;
        public MainWindow mainHandle;
        private string compilerVersionPath;

        public GDB(MainWindow passedHandle, int OCDNum)
        {
            mainHandle = passedHandle;
            ProcessStartInfo GDBInfo = new ProcessStartInfo();
            GDBProcess = new Process();

            switch (passedHandle.textGCCVersion)
            {
                case "GCC4.7":
                    compilerVersionPath = "GCC4.7.3";
                    break;
                case "GCC4.9":
                    compilerVersionPath = "GCC4.9.3";
                    break;
                case "GCC5.4":
                    compilerVersionPath = "GCC5.4.1";
                    break;
                case "GCC6.3":
                    compilerVersionPath = "GCC6.3.1";
                    break;
                default:
                    break;
            }
            
            System.Diagnostics.Debug.WriteLine("Starting GDB.");

            GDBInfo.CreateNoWindow = true;
            GDBInfo.RedirectStandardInput = true;
            GDBInfo.UseShellExecute = false;
            GDBInfo.RedirectStandardOutput = true;
            GDBInfo.RedirectStandardError = true;
            GDBInfo.Arguments = @"-quiet -fullname --interpreter=mi2";
            GDBInfo.FileName = mainHandle.textCompilerPath + @"\" + compilerVersionPath + @"\" + @"\bin\arm-none-eabi-gdb.exe";

            GDBProcess.OutputDataReceived += new DataReceivedEventHandler(StandardOutputHandler);
            GDBProcess.ErrorDataReceived += new DataReceivedEventHandler(StandardErrorHandler);

            GDBProcess.StartInfo = GDBInfo;
            GDBProcess.Start();
            input = GDBProcess.StandardInput;
            GDBProcess.BeginOutputReadLine();
            GDBProcess.BeginErrorReadLine();

            if (RunCommand(@"target remote localhost:" + passedHandle.interfaceJTAG.getGDBPort(OCDNum), "Remote debugging using localhost:" + passedHandle.interfaceJTAG.getGDBPort(OCDNum), "^error", 5000) != CommandStatus.Done)
            {
                System.Diagnostics.Debug.WriteLine("GDB failed to connect to localhost.");                
            }
            if (RunCommand(@"monitor soft_reset_halt", "target halted due to breakpoint", "^error", 5000) != CommandStatus.Done)
            {
                System.Diagnostics.Debug.WriteLine("GDB failed to halt processor.");
                gdbConnnected = false;
            }
            else
            {
                gdbConnnected = true;
            }            
        }

        public bool Load(string axfFile)
        {
            string modifiedAXFFile = axfFile.Replace(@"\", @"\\");
            System.Diagnostics.Debug.WriteLine("GDB loading: axf: " + modifiedAXFFile);
            waitForMessages();

            /*if (RunCommand(@"monitor mwb 0x20000000 0 0x18000", "^done", "^error", 15000) != CommandStatus.Done)
            {
                System.Diagnostics.Debug.WriteLine("GDB failed to clear memory.");
                return false;
            }*/
            if (RunCommand(@"-file-exec-and-symbols " + modifiedAXFFile, "^done", "^error", 10000) != CommandStatus.Done)
            {
                System.Diagnostics.Debug.WriteLine("GDB failed to run -file-exec-and-symbols command.");
                return false;
            }
            if (RunCommand(@"load", "Transfer rate", "^error", 900000) != CommandStatus.Done)
            {
                System.Diagnostics.Debug.WriteLine("GDB failed to load file.");
                return false;
            }

            return true;
        }

        public bool Continue()
        {
            System.Diagnostics.Debug.WriteLine("GDB continue");
            if (RunCommand(@"monitor soft_reset_halt", "target halted due to breakpoint", "^error", 3000) != CommandStatus.Done)
            {
                System.Diagnostics.Debug.WriteLine("GDB failed to halt processor.");
                return false;
            }
            if (RunCommand(@"-exec-continue", "^running", "^error", 1000) != CommandStatus.Done)
            {
                System.Diagnostics.Debug.WriteLine("GDB failed to halt processor.");
                return false;
            }

            return true;
        }

        public void Kill()
        {
            try
            {
                RunCommand("quit");
                if (GDBProcess != null) {
                    GDBProcess.Kill();
                }
                System.Diagnostics.Debug.WriteLine("GDBProcess killed.");
                GDBProcess = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("GDBProcess already killed. Can't kill again: " + ex.ToString());
            }
            gdbConnnected = false;
        }

        private void ProcessResponse(string response)
        {
            if ((expectedPassResponse != String.Empty) && (expectedPassResponse != null))
            {
                if (response.Contains(expectedPassResponse))
                {
                    commandResult = CommandStatus.Done;
                    ARE_result.Set();
                }
            }
            if ((expectedFailResponse != String.Empty) && (expectedFailResponse != null))
            {
                if (response.Contains(expectedFailResponse))
                {
                    commandResult = CommandStatus.Error;
                    ARE_result.Set();
                }
            }
        }

        private void StandardOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            //System.Diagnostics.Debug.WriteLine("******************GDB command result: " + outLine.Data);
            if (!String.IsNullOrEmpty(outLine.Data))
            {
                ProcessResponse(outLine.Data);
            }
        }

        private void StandardErrorHandler(object sendingProcess, DataReceivedEventArgs errLine)
        {
            //System.Diagnostics.Debug.WriteLine("******************GDB error: " + errLine.Data);
            if (!String.IsNullOrEmpty(errLine.Data))
            {
                ProcessResponse(errLine.Data);
            }
        }        

        private bool RunCommand(string command)
        {
            System.Diagnostics.Debug.WriteLine("GDB run command: " + command);
            input.WriteLine(command);

            return true;
        }

        private bool waitForMessages()
        {
            expectedPassResponse = "This string should never be matched in the GDB output";
            ARE_result.WaitOne(500);
            return true;
        }

        private CommandStatus RunCommand(string command, string expectPass, string expectFail, int timeout)
        {
            int attempts;

            expectedPassResponse = expectPass;
            expectedFailResponse = expectFail;

            for (attempts = 0; attempts < 3; attempts++)
            {
                System.Diagnostics.Debug.WriteLine("GDB run attempt " + attempts.ToString() + " for: " + command + " waiting for: " + expectPass.ToString());
                commandResult = CommandStatus.Running;
                input.WriteLine(command);
                ARE_result.WaitOne(timeout);
                if (commandResult == CommandStatus.Done)
                    break;
            }
            System.Diagnostics.Debug.WriteLine("GDB: waiting for messages.");
            // attempt to purge out any queued up "^done"
            waitForMessages();
            System.Diagnostics.Debug.WriteLine("GDB done waiting: " + command + " complete");
            return commandResult;
        }
    }
}
