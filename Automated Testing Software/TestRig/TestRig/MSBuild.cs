using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Linq;

namespace TestRig
{
    public class MSBuild
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

        private StringWriter stdOutput = new StringWriter();
        public StringWriter Output { get { return stdOutput; } }
        private StringWriter stdError = new StringWriter();
        public StringWriter Error { get { return stdError; } }

        private static AutoResetEvent ARE_result = new AutoResetEvent(false);

        private Process MSBuildProcess;
        public MainWindow mainHandle;

        private string MFPath;
        private string usingMFVersion;

        public MSBuild(MainWindow passedHandle, string MFVersion)
        {
            mainHandle = passedHandle;
            ProcessStartInfo MSBuildInfo = new ProcessStartInfo();
            MSBuildProcess = new Process();
            
            System.Diagnostics.Debug.WriteLine("Starting MSBuild.");

            MSBuildInfo.CreateNoWindow = true;
            MSBuildInfo.RedirectStandardInput = true;
            MSBuildInfo.UseShellExecute = false;
            MSBuildInfo.RedirectStandardOutput = true;
            MSBuildInfo.RedirectStandardError = true;
            MSBuildInfo.WindowStyle = ProcessWindowStyle.Hidden;
            MSBuildInfo.FileName = @"cmd.exe";

            MSBuildProcess.OutputDataReceived += new DataReceivedEventHandler(StandardOutputHandler);
            MSBuildProcess.ErrorDataReceived += new DataReceivedEventHandler(StandardErrorHandler);

            MSBuildProcess.StartInfo = MSBuildInfo;
            MSBuildProcess.Start();
            input = MSBuildProcess.StandardInput;
            MSBuildProcess.BeginOutputReadLine();
            MSBuildProcess.BeginErrorReadLine();

            usingMFVersion = MFVersion;
            switch (usingMFVersion)
            {
                case "4.0":
                    MFPath = mainHandle.textMFPath_4_0;
                    break;
                case "4.3":
                    MFPath = mainHandle.textMFPath_4_3;
                    break;
                default:
                    MFPath = mainHandle.textMFPath_4_3;
                    break;
            }
        }

        private bool ChangeDirectories(string directory)
        {
            // first change disks (might be needed)
            if (directory.Contains(':'))
            {
                int index = directory.IndexOf(':');
                string strippedName = directory.Substring(0, index + 1);

                // changing disks
                RunCommand(strippedName);
                // changing directory
                RunCommand(@"cd " + directory);
            }
            else
            {
                // no need to change disks, just directory
                RunCommand(@"cd " + directory);
            }

            return true;
        }

        public bool BuildTinyCLR(TestDescription currentTest)
        {
            ChangeDirectories(MFPath);
            /*if (RunCommand(@"setenv_gcc.cmd " + mainHandle.textBuildSourceryPath, "setting vars", String.Empty, 1000) != CommandStatus.Done)
            {
                System.Diagnostics.Debug.WriteLine("MSBuild failed to setenv.");
                return false;
            }*/
            switch (usingMFVersion)
            {
                case "4.0":
                    RunCommand(@"setenv_base.cmd " + currentTest.testGCCVersion + " PORT " + mainHandle.textBuildSourceryPath);
                    break;
                case "4.3":
                    RunCommand(@"setenv_gcc.cmd 4.2.1 " + mainHandle.textBuildSourceryPath);
                    break;
                default:
                    RunCommand(@"setenv_gcc.cmd 4.2.1 " + mainHandle.textBuildSourceryPath);
                    break;
            }

            ChangeDirectories(MFPath + @"\Solutions\" + currentTest.testSolution + @"\" + currentTest.testSolutionType);            
             
            if (RunCommand(@"msbuild /t:clean /p:memory=" + currentTest.testMemoryType + " " + currentTest.testSolutionType + ".proj", "Build succeeded", "Build FAILED", 20000) != CommandStatus.Done)
            {
                System.Diagnostics.Debug.WriteLine("MSBuild failed to clean.");
                return false;
            }
            else
                System.Diagnostics.Debug.WriteLine("MSBuild project cleaned.");

            //if (RunCommand(@"msbuild /t:build /p:configuration=Release /p:memory=" + currentTest.testMemoryType + " " + currentTest.testSolutionType + ".proj", "Build succeeded", "Build FAILED", 900000) != CommandStatus.Done)
            if (RunCommand(@"msbuild /t:build /p:memory=" + currentTest.testMemoryType + " " + currentTest.testSolutionType + ".proj", "Build succeeded", "Build FAILED", 900000) != CommandStatus.Done)
            {
                System.Diagnostics.Debug.WriteLine("MSBuild failed to build.");
                return false;
            }
            else
                System.Diagnostics.Debug.WriteLine("MSBuild project built.");

            return true;
        }

        public bool BuildNativeProject(string path, string project, TestDescription currentTest)
        {
            ChangeDirectories(MFPath);

            string fullPath = mainHandle.textTestSourcePath;
            int index = fullPath.LastIndexOf(@"TestSys");
            string strippedPath = path.Substring(0, index);
            RunCommand(@"SET TESTSOURCE=" + strippedPath);
            switch (usingMFVersion)
            {
                case "4.0":
                    RunCommand(@"setenv_base.cmd " + currentTest.testGCCVersion + " PORT " + mainHandle.textBuildSourceryPath);
                    break;
                case "4.3":
                    RunCommand(@"setenv_gcc.cmd 4.2.1 " + mainHandle.textBuildSourceryPath);
                    break;
                default:
                    RunCommand(@"setenv_gcc.cmd 4.2.1 " + mainHandle.textBuildSourceryPath);
                    break;
            }
            ChangeDirectories(path);            
            if (RunCommand(@"msbuild /t:clean /p:memory=" + currentTest.testMemoryType + " " + project, "Build succeeded", "Build FAILED", 20000) != CommandStatus.Done)
            {
                System.Diagnostics.Debug.WriteLine("MSBuild failed to clean.");
                return false;
            }
            else
                System.Diagnostics.Debug.WriteLine("MSBuild project cleaned.");
            if (RunCommand(@"msbuild /t:build /p:memory=" + currentTest.testMemoryType + " " + project, "Build succeeded", "Build FAILED", 900000) != CommandStatus.Done)
            {
                System.Diagnostics.Debug.WriteLine("MSBuild failed to build.");
                return false;
            } 
            else
                System.Diagnostics.Debug.WriteLine("MSBuild project built.");

            return true;
        }

        public bool BuildManagedProject(string path, string project, TestDescription currentTest)
        {
            string applicationStartAddress;

            switch (currentTest.testSolution){
                case "STM32F10x":
                    applicationStartAddress = "80A2000";
                    break;
                case "EmoteDotNow":
                    applicationStartAddress = "80A2000";
                    break;
                case "SOC8200":
                    applicationStartAddress = "80A2000";
                    break;
                case "SOC_ADAPT":
                    applicationStartAddress = "805E8000";
                    break;
                default:
                    applicationStartAddress = "80A2000";
                    break;
            }

            ChangeDirectories(MFPath);

            string fullPath = mainHandle.textTestSourcePath;
            int index = fullPath.LastIndexOf(@"TestSys");
            string strippedPath = path.Substring(0, index);
            RunCommand(@"SET TESTSOURCE=" + strippedPath);
            switch (usingMFVersion)
            {
                case "4.0":
                    RunCommand(@"setenv_base.cmd " + currentTest.testGCCVersion + " PORT " + mainHandle.textBuildSourceryPath);
                    break;
                case "4.3":
                    RunCommand(@"setenv_gcc.cmd 4.2.1 " + mainHandle.textBuildSourceryPath);
                    break;
                default:
                    RunCommand(@"setenv_gcc.cmd 4.2.1 " + mainHandle.textBuildSourceryPath);
                    break;
            }
            ChangeDirectories(path);
            if (RunCommand(@"msbuild /t:clean /p:memory=" + currentTest.testMemoryType + " " + project, "Build succeeded", "Build FAILED", 20000) != CommandStatus.Done)
            {
                System.Diagnostics.Debug.WriteLine("MSBuild failed to clean.");
                return false;
            }
            else
                System.Diagnostics.Debug.WriteLine("MSBuild project cleaned.");
            // msbuild /t:build /p:memory=" + currentTest.testMemoryType + @" (default) (/p:memory=RAM)
            if (RunCommand(@"msbuild /t:build /p:memory=" + currentTest.testMemoryType + " " + project, "Build succeeded", "Build FAILED", 900000) != CommandStatus.Done)
            {
                System.Diagnostics.Debug.WriteLine("MSBuild failed to build.");
                return false;
            }
            else
                System.Diagnostics.Debug.WriteLine("MSBuild project built.");

            // stripping the name of the project we are compiling to rename it to *.s19 and *_conv.s19 files
            string projectName = project;
            index = projectName.LastIndexOf('.');
            string strippedName = projectName.Substring(0, index);
            //string buildOutput = @"\BuildOutput\public\Debug\Client\dat\";
            string buildOutput = @"bin\Release\";            
            // convert to S19 record
            if (RunCommand(@"binToSrec.exe -b " + applicationStartAddress + " -i " + buildOutput + strippedName + ".dat -o " + buildOutput + strippedName + ".s19", "Conversion is Successful", "FAILED", 10000) != CommandStatus.Done)
            {
                System.Diagnostics.Debug.WriteLine("MSBuild failed to convert to S19 step 1.");
                return false;
            }
            else
                System.Diagnostics.Debug.WriteLine("MSBuild project converted to S19 step 1.");
            RunCommand(mainHandle.textBuildSourceryPath + @"\bin\arm-none-eabi-objcopy.exe " + buildOutput + strippedName + @".s19 " + buildOutput + strippedName + @"_Conv.s19");
            Thread.Sleep(2000);

            return true;
        }

        public void Kill()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("MSBuildProcess killed.");
                MSBuildProcess.Kill();
                MSBuildProcess = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("MSBuildProcess already killed. Can't kill again: " + ex.ToString());
            }
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
            System.Diagnostics.Debug.WriteLine("******************MSBuild command result: " + outLine.Data);
            if (!String.IsNullOrEmpty(outLine.Data))
            {
                ProcessResponse(outLine.Data);
            }
        }

        private void StandardErrorHandler(object sendingProcess, DataReceivedEventArgs errLine)
        {
            System.Diagnostics.Debug.WriteLine("******************MSBuild error: " + errLine.Data);
            if (!String.IsNullOrEmpty(errLine.Data))
            {
                ProcessResponse(errLine.Data);
            }
        }        

        private static string Escape(string str)
        {
            if (str == null)
                return null;
            else if (str.IndexOf(' ') != -1 || str.IndexOf('"') != -1)
            {
                str = str.Replace("\"", "\\\"");
                return "\"" + str + "\"";
            }
            else
                return str;
        }

        private bool RunCommand(string command)
        {
            System.Diagnostics.Debug.WriteLine("MSBuild run command: " + command);
            input.WriteLine(command);

            return true;
        }

        private CommandStatus RunCommand(string command, string expectPass, string expectFail, int timeout)
        {
            int attempts = 0;
            expectedPassResponse = expectPass;
            expectedFailResponse = expectFail;

            //for (attempts = 0; attempts < 3; attempts++)
            //{
                System.Diagnostics.Debug.WriteLine("MSBuild run attempt " + attempts.ToString() + " for: " + command + " waiting for: " + expectedPassResponse.ToString());
                commandResult = CommandStatus.Running;
                input.WriteLine(command);
                ARE_result.WaitOne(timeout);
                if (commandResult == CommandStatus.Done)
                    return commandResult;
            //}
            return commandResult;
        }
    }
}