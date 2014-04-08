using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace TestRig
{
    public class Fastboot
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

        private Process FastbootProcess;
        public MainWindow mainHandle;
        public string testPowerAutomateSelected;

        public Fastboot(MainWindow passedHandle, string testPowerAutomateSelected)
        {
            mainHandle = passedHandle;
            this.testPowerAutomateSelected = testPowerAutomateSelected;
            ProcessStartInfo FastbootInfo = new ProcessStartInfo();
            FastbootProcess = new Process();

            System.Diagnostics.Debug.WriteLine("Starting Fastboot.");

            FastbootInfo.CreateNoWindow = true;
            FastbootInfo.RedirectStandardInput = true;
            FastbootInfo.UseShellExecute = false;
            FastbootInfo.RedirectStandardOutput = true;
            FastbootInfo.RedirectStandardError = true;
            FastbootInfo.FileName = @"cmd.exe";

            FastbootProcess.OutputDataReceived += new DataReceivedEventHandler(StandardOutputHandler);
            FastbootProcess.ErrorDataReceived += new DataReceivedEventHandler(StandardErrorHandler);

            FastbootProcess.StartInfo = FastbootInfo;
            FastbootProcess.Start();
            input = FastbootProcess.StandardInput;
            FastbootProcess.BeginOutputReadLine();
            FastbootProcess.BeginErrorReadLine();                        
        }

        public bool enterFastbootMode()
        {
            // enter fastboot mode on board
            System.Diagnostics.Debug.WriteLine("Entering fastboot mode");
            if (testPowerAutomateSelected.Equals(false.ToString())) {
                MainWindow.showMessageBox("Enter fastboot (hold Vol-, press RESET) then click OK"); //delete after tbd
                // TBD power toggle with vol - down
            }
            else {
                int[] output = new int[7];
                int model, pwrUsbConnected = 0;
                StringBuilder firmware = new StringBuilder(128);

                for (int i = 0; i < 7; i++)
                    output[i] = 0;

                // Initialize the PowerUSB or else quit
                if (PwrUSBWrapper.InitPowerUSB(out model, firmware) > 0) {
                    Console.Write("PowerUSB Connected. Model:{0:D}  Firmware:", model);
                    Console.WriteLine(firmware);
                    pwrUsbConnected = PwrUSBWrapper.CheckStatusPowerUSB();
                }
                if (pwrUsbConnected == 0) {
                    System.Diagnostics.Debug.WriteLine("PowerUSB is not connected\r\n");
                    return false;
                }
                PwrUSBWrapper.SetCurrentPowerUSB(0);

                System.Diagnostics.Debug.WriteLine("PowerUSB Connected. Model: " + model.ToString() + ". Version: " + firmware.ToString());
                System.Diagnostics.Debug.WriteLine("Turning off USB power strip outlet 1");
                PwrUSBWrapper.SetPortPowerUSB(0, 0, 0);

                // 'pushing' button vol -
                //output[0] = 1;	// o3
                //PwrUSBWrapper.SetOutputStatePowerUSB(output);
                Thread.Sleep(4000);

                System.Diagnostics.Debug.WriteLine("Turning on USB power strip outlet 1");
                PwrUSBWrapper.SetPortPowerUSB(1, 0, 0);
                Thread.Sleep(8000);

                // release button vol -
                //output[0] = 0;	// o3
                //PwrUSBWrapper.SetOutputStatePowerUSB(output);

                PwrUSBWrapper.ClosePowerUSB();
            }
            if (RunCommand(@"fastboot devices", "fastboot", "error", 10000) != CommandStatus.Done)
            {
                System.Diagnostics.Debug.WriteLine("Fastboot failed to connect to board.");
                return false;
            }
            return true;

        }

        public bool createFinalBinary(string inFile1Name, string inFile2Name, string concatFileName)
        {
            try
            {
                int padFile1Size = 3 * 1024 * 1024;
                int padFile2Size = 2 * 1024 * 1024;
                string padFile1Name = "f1_pad.bin";
                string padFile2Name = "f2_pad.bin";

                padFile(inFile1Name, padFile1Name, padFile1Size);
                padFile(inFile2Name, padFile2Name, padFile2Size);

                concatFiles(padFile1Name, padFile2Name, concatFileName);

                File.Delete(padFile1Name);
                File.Delete(padFile2Name);
            }
            catch (Exception ex)
            {
                Console.WriteLine("createFinalBinary fail: " + ex.Message);
                return false;
            }
            return true;
        }

        private bool padFile(string inFileName, string padFileName, int padFileSize)
        {
            try
            {
                byte[] inFileData = File.ReadAllBytes(inFileName);

                byte[] padFileData = new byte[padFileSize];

                for (int i = 0; i < padFileSize; i++)
                {
                    padFileData[i] = 0;
                }

                inFileData.CopyTo(padFileData, 0);

                BinaryWriter bWrite = new BinaryWriter(File.Open(padFileName, FileMode.Create));
                bWrite.Write(padFileData, 0, padFileSize);
                bWrite.Close();
                System.Diagnostics.Debug.WriteLine("Created padded file: " + padFileName + " from binary file: " + inFileName);
                FileInfo fileInfo = new FileInfo(padFileName);
                System.Diagnostics.Debug.WriteLine("Padded file size should be: " + padFileSize.ToString() + " bytes and actual size is: " + fileInfo.Length.ToString() + " bytes");
            }
            catch (Exception ex)
            {
                Console.WriteLine("padFile fail: " + ex.Message);
                return false;
            }
            return true;
        }

        private bool concatFiles(string file1, string file2, string concatFile)
        {
            try
            {
                byte[] inFile1Data = File.ReadAllBytes(file1);
                byte[] inFile2Data = File.ReadAllBytes(file2);

                byte[] concatFileData = new byte[inFile1Data.Length + inFile2Data.Length];

                int j = 0;
                for (int i = 0; i < inFile1Data.Length; i++)
                {
                    concatFileData[j++] = inFile1Data[i];
                }
                for (int i = 0; i < inFile2Data.Length; i++)
                {
                    concatFileData[j++] = inFile2Data[i];
                }

                BinaryWriter bWrite = new BinaryWriter(File.Open(concatFile, FileMode.Create));
                bWrite.Write(concatFileData, 0, concatFileData.Length);
                bWrite.Close();
                System.Diagnostics.Debug.WriteLine("Created concated file: " + concatFile);
                FileInfo file1Info = new FileInfo(file1);
                FileInfo file2Info = new FileInfo(file2);
                FileInfo concatInfo = new FileInfo(concatFile);
                System.Diagnostics.Debug.WriteLine("Concat file size should be: " + (file1Info.Length + file2Info.Length).ToString() + " bytes and actual size is: " + concatInfo.Length.ToString() + " bytes");
            }
            catch (Exception ex)
            {
                Console.WriteLine("concatFiles fail: " + ex.Message);
                return false;
            }
            return true;
        }
        
        public bool load(string binaryFileName)
        {
            System.Diagnostics.Debug.WriteLine("Fastboot loading: " + binaryFileName);
            waitForMessages();

            if (RunCommand(@"fastboot flash boot " + binaryFileName, "finished", "error", 15000) != CommandStatus.Done)
            {
                System.Diagnostics.Debug.WriteLine("Fastboot failed to load: " + binaryFileName);
                return false;
            }

            return true;
        }

        public bool run()
        {
            // enter app mode
            System.Diagnostics.Debug.WriteLine("Entering app mode");
            if (testPowerAutomateSelected.Equals(false.ToString())) {
                MainWindow.showMessageBox("Enter MF mode then click OK"); //delete after tbd
                // TBD power toggle with vol + down
            }
            else {
                int[] output = new int[7];
                int model, pwrUsbConnected = 0;
                StringBuilder firmware = new StringBuilder(128);

                for (int i = 0; i < 7; i++)
                    output[i] = 0;

                // Initialize the PowerUSB or else quit
                if (PwrUSBWrapper.InitPowerUSB(out model, firmware) > 0) {
                    System.Diagnostics.Debug.WriteLine("PowerUSB Connected. Model: " + model.ToString() + ". Version: " + firmware.ToString());
                    Console.WriteLine(firmware);
                    pwrUsbConnected = PwrUSBWrapper.CheckStatusPowerUSB();
                }
                if (pwrUsbConnected == 0) {
                    System.Diagnostics.Debug.WriteLine("PowerUSB is not connected\r\n");
                    return false;
                }
                PwrUSBWrapper.SetCurrentPowerUSB(0);

                System.Diagnostics.Debug.WriteLine("PowerUSB Connected. Model: " + model.ToString() + ". Version: " + firmware.ToString());
                System.Diagnostics.Debug.WriteLine("Turning off USB power strip outlet 1");
                PwrUSBWrapper.SetPortPowerUSB(0, 0, 0);

                // 'pushing' button vol +
                output[1] = 1;	// o2
                PwrUSBWrapper.SetOutputStatePowerUSB(output);
                Thread.Sleep(4000);

                System.Diagnostics.Debug.WriteLine("Turning on USB power strip outlet 1");
                PwrUSBWrapper.SetPortPowerUSB(1, 0, 0);
                Thread.Sleep(8000);

                // release button vol +
                output[1] = 0;	// o2
                PwrUSBWrapper.SetOutputStatePowerUSB(output);

                PwrUSBWrapper.ClosePowerUSB();
            }
            return true;
        }

        public void Kill()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Fastboot process killed.");
                FastbootProcess.Kill();
                FastbootProcess = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Fastboot process already killed. Can't kill again: " + ex.ToString());
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
            System.Diagnostics.Debug.WriteLine("******************Fastboot command result: " + outLine.Data);
            if (!String.IsNullOrEmpty(outLine.Data))
            {
                ProcessResponse(outLine.Data);
            }
        }

        private void StandardErrorHandler(object sendingProcess, DataReceivedEventArgs errLine)
        {
            System.Diagnostics.Debug.WriteLine("******************Fastboot error: " + errLine.Data);
            if (!String.IsNullOrEmpty(errLine.Data))
            {
                ProcessResponse(errLine.Data);
            }
        }

        private bool RunCommand(string command)
        {
            System.Diagnostics.Debug.WriteLine("Fastboot run command: " + command);
            input.WriteLine(command);

            return true;
        }

        private bool waitForMessages()
        {
            expectedPassResponse = "This string should never be matched in the Fastboot output";
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
                System.Diagnostics.Debug.WriteLine("Fastboot run attempt " + attempts.ToString() + " for: " + command + " waiting for: " + expectPass.ToString());
                commandResult = CommandStatus.Running;
                input.WriteLine(command);
                ARE_result.WaitOne(timeout);
                if (commandResult == CommandStatus.Done)
                    break;
            }
            System.Diagnostics.Debug.WriteLine("Fastboot: waiting for messages.");
            waitForMessages();
            System.Diagnostics.Debug.WriteLine("Fastboot done waiting: " + command + " complete");
            return commandResult;
        }
    }
}
