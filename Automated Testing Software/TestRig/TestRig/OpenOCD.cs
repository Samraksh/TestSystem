using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace TestRig
{    
    public class OpenOCD
    {
        public enum CommandStatus
        {
            Done,
            Running,
            Error
        }

        private CommandStatus commandResult;
        private string expectedResponse = String.Empty;
        public StreamWriter input = null;

        private StringWriter stdOutput = new StringWriter();
        public StringWriter Output { get { return stdOutput; } }
        private StringWriter stdError = new StringWriter();
        public StringWriter Error { get { return stdError; } }

        private static AutoResetEvent ARE_result = new AutoResetEvent(false);

        private Process OCDProcess;        
        public MainWindow mainHandle;

        public OpenOCD(MainWindow passedHandle)
        {
            mainHandle = passedHandle;
            ProcessStartInfo openOCDInfo = new ProcessStartInfo();
            OCDProcess = new Process();

            System.Diagnostics.Debug.WriteLine("Starting OpenOCD.");

            openOCDInfo.CreateNoWindow = true;
            openOCDInfo.RedirectStandardOutput = true;
            openOCDInfo.RedirectStandardInput = true;
            openOCDInfo.UseShellExecute = false;
            openOCDInfo.RedirectStandardError = true;
            openOCDInfo.WorkingDirectory = Path.GetDirectoryName(mainHandle.textOCDExe);
            openOCDInfo.Arguments = @"-f " + mainHandle.textOCDInterface + " -f " + mainHandle.textOCDTarget;
            openOCDInfo.FileName = mainHandle.textOCDExe;

            OCDProcess.OutputDataReceived += new DataReceivedEventHandler(StandardOutputHandler);
            OCDProcess.ErrorDataReceived += new DataReceivedEventHandler(StandardErrorHandler);

            OCDProcess.StartInfo = openOCDInfo;
            OCDProcess.Start();
            OCDProcess.BeginOutputReadLine();
            OCDProcess.BeginErrorReadLine();
        }

        public void Kill()
        {
            try{
                OCDProcess.Kill();
                OCDProcess = null;
                System.Diagnostics.Debug.WriteLine("OCDProcess killed.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("OCDProcess already killed. Can't kill again: " + ex.ToString());
            }
        }

        private void ProcessResponse(string response)
        {
            if (response.Contains(expectedResponse))
            {
                commandResult = CommandStatus.Done; 
                ARE_result.Set();
            }
        }

        private void StandardOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("------OCDstd--------> " + outLine.Data.ToString());
                if (!String.IsNullOrEmpty(outLine.Data))
                {
                    ProcessResponse(outLine.Data);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("OpenOCD StandardOutputHandler exception: " + ex.Message);
            }
        }

        private void StandardErrorHandler(object sendingProcess, DataReceivedEventArgs errLine)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("------OCDerr--------> " + errLine.Data.ToString());
                if (!String.IsNullOrEmpty(errLine.Data))
                {
                    ProcessResponse(errLine.Data);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("OpenOCD StandardErrorHandler exception: " + ex.Message);
            }
        }

        private bool RunCommand(string command)
        {
            System.Diagnostics.Debug.WriteLine("GDB run command: " + command);
            input.WriteLine(command);

            return true;
        }

        private CommandStatus RunCommand(string command, string expect, int timeout)
        {
            int attempts;
            expectedResponse = expect;

            for (attempts = 0; attempts < 3; attempts++)
            {
                System.Diagnostics.Debug.WriteLine("GDB run attempt " + attempts.ToString() + " for: " + command + " waiting for: " + expect.ToString());
                commandResult = CommandStatus.Running;
                input.WriteLine(command);
                ARE_result.WaitOne(timeout);
                if (commandResult == CommandStatus.Done)
                    return commandResult;
            }
            return commandResult;
        }
    }
}
