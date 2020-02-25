﻿using System;
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
        private string compilerVersion;
        private string compilerVersionPath;

        private string applicationStartAddress;
        private bool applicationStartAddressFound = false;
        private string preprocessorString;

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
                case "H7_4.3":
                    MFPath = mainHandle.textMFPath_H7_4_3;
                    break;
                default:
                    MFPath = mainHandle.textMFPath_4_3;
                    break;
            }
            switch (passedHandle.textGCCVersion)
            {
                case "GCC4.7":
                    compilerVersion = "4.7.3";
                    compilerVersionPath = "GCC4.7.3";
                    break;
                case "GCC4.9":
                    compilerVersion = "4.9.3";
                    compilerVersionPath = "GCC4.9.3";
                    break;
                case "GCC5.4":
                    compilerVersion = "5.4.1";
                    compilerVersionPath = "GCC5.4.1";
                    break;
                case "GCC6.3":
                    compilerVersion = "6.3.1";
                    compilerVersionPath = "GCC6.3.1";
                    break;
                default:
                    break;
            }

            applicationStartAddress = String.Empty;
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

        public bool BuildTinyCLR(TestDescription currentTest, bool cleanBuildNeeded)
        {
            StreamReader sr;
            String line;

            ChangeDirectories(MFPath);

            switch (usingMFVersion)
            {
                case "4.0":
                    RunCommand(@"setenv_base.cmd " + currentTest.testGCCVersion + " PORT " + mainHandle.textCompilerPath + @"\" + compilerVersionPath);
                    break;
                case "4.3":
                    RunCommand(@"setenv_gcc.cmd " + compilerVersion + " " + mainHandle.textCompilerPath + @"\" + compilerVersionPath);
                    break;
                default:
                    RunCommand(@"setenv_gcc.cmd " + compilerVersion + " " + mainHandle.textCompilerPath + @"\" + compilerVersionPath);
                    break;
            }

            ChangeDirectories(MFPath + @"\Solutions\" + currentTest.testSolution + @"\" + currentTest.testSolutionType);

            // we only clean TinyBooter, not TinyCLR (it would clean TinyBooter also)
            if (currentTest.testSolutionType == "TinyBooter")
            {
                if (cleanBuildNeeded)
                {
                    if (mainHandle.textCodeBuildSelected.Contains("Release"))
                    {
                        if (RunCommand(@"msbuild /maxcpucount /t:clean /p:memory=" + currentTest.testMemoryType + ",flavor=release " + currentTest.testSolutionType + ".proj", "Build succeeded", "Build FAILED", 20000) != CommandStatus.Done)
                        {
                            System.Diagnostics.Debug.WriteLine("MSBuild failed to clean.");
                            return false;
                        }
                        else
                            System.Diagnostics.Debug.WriteLine("MSBuild project cleaned.");
                    }
                    else
                    {
                        if (RunCommand(@"msbuild /maxcpucount /t:clean /p:memory=" + currentTest.testMemoryType + ",flavor=debug " + currentTest.testSolutionType + ".proj", "Build succeeded", "Build FAILED", 20000) != CommandStatus.Done)
                        {
                            System.Diagnostics.Debug.WriteLine("MSBuild failed to clean.");
                            return false;
                        }
                        else
                            System.Diagnostics.Debug.WriteLine("MSBuild project cleaned.");
                    }
                }

                if (mainHandle.textCodeBuildSelected.Contains("Release"))
                {
                    if (RunCommand(@"msbuild /maxcpucount /t:build /p:memory=" + currentTest.testMemoryType + ",flavor=release  " + currentTest.testSolutionType + ".proj", "Build succeeded", "Build FAILED", 900000) != CommandStatus.Done)
                    {
                        System.Diagnostics.Debug.WriteLine("MSBuild failed to build release TinyBooter.");
                        return false;
                    }
                    else
                        System.Diagnostics.Debug.WriteLine("MSBuild project built release TinyBooter.");
                }
                else
                {
                    if (RunCommand(@"msbuild /maxcpucount /t:build /p:memory=" + currentTest.testMemoryType + ",flavor=debug " + currentTest.testSolutionType + ".proj", "Build succeeded", "Build FAILED", 900000) != CommandStatus.Done)
                    {
                        System.Diagnostics.Debug.WriteLine("MSBuild failed to build debug TinyBooter.");
                        return false;
                    }
                    else
                        System.Diagnostics.Debug.WriteLine("MSBuild project built debug TinyBooter.");
                }
            }
            else
            {
                if (cleanBuildNeeded)
                {
                    if (mainHandle.textCodeBuildSelected.Contains("Release"))
                    {
                        if (RunCommand(@"msbuild /maxcpucount /t:clean /p:memory=" + currentTest.testMemoryType + ",flavor=release  " + currentTest.testSolutionType + ".proj", "Build succeeded", "Build FAILED", 20000) != CommandStatus.Done)
                        {
                            System.Diagnostics.Debug.WriteLine("release build failed to clean.");
                            return false;
                        }
                        else
                            System.Diagnostics.Debug.WriteLine("release build project cleaned.");
                    }
                    else
                    {
                        if (RunCommand(@"msbuild /maxcpucount /t:clean /p:memory=" + currentTest.testMemoryType + ",flavor=debug " + currentTest.testSolutionType + ".proj", "Build succeeded", "Build FAILED", 20000) != CommandStatus.Done)
                        {
                            System.Diagnostics.Debug.WriteLine("release build failed to clean.");
                            return false;
                        }
                        else
                            System.Diagnostics.Debug.WriteLine("release build project cleaned.");
                    }
                }
                if (mainHandle.textCodeBuildSelected.Contains("Release"))
                {
                    if (RunCommand(@"msbuild /t:build /p:memory=" + currentTest.testMemoryType + ",flavor=release  " + currentTest.testSolutionType + ".proj", "Build succeeded", "Build FAILED", 900000) != CommandStatus.Done)                    
                    {
                        System.Diagnostics.Debug.WriteLine("MSBuild Release failed to build.");
                        return false;
                    }
                    else
                        System.Diagnostics.Debug.WriteLine("MSBuild Release project built.");
                }
                else
                {
                    if (RunCommand(@"msbuild /maxcpucount /t:build /p:memory=" + currentTest.testMemoryType + ",flavor=debug " + currentTest.testSolutionType + ".proj", "Build succeeded", "Build FAILED", 900000) != CommandStatus.Done)
                    {
                        System.Diagnostics.Debug.WriteLine("MSBuild Debug failed to build.");
                        return false;
                    }
                    else
                        System.Diagnostics.Debug.WriteLine("MSBuild Debug project built.");
                }
            }
            System.Diagnostics.Debug.WriteLine("Searching scatterfile");
            // discovering applicationStartAddress value
            applicationStartAddressFound = false;
            string FileName;
            if (currentTest.testSolutionType == "TinyBooter")
                FileName = MFPath + @"\Solutions\" + currentTest.testSolution + @"\" + currentTest.testSolutionType + @"\" + "scatterfile_bootloader_gcc.xml";
            else if (currentTest.testSolution == "SmartFusion2")
                FileName = MFPath + @"\Solutions\" + currentTest.testSolution + @"\" + currentTest.testSolutionType + @"\" + "scatterfile_emoteOS_gcc.xml";
            else 
                FileName = MFPath + @"\Solutions\" + currentTest.testSolution + @"\" + currentTest.testSolutionType +  @"\" + "scatterfile_tinyclr_gcc.xml";
            if (File.Exists(FileName) == true)
            {
                sr = new StreamReader(FileName);
                line = sr.ReadLine();
                //advance to addresses of correct solution type.
                while (line != null && !line.Contains("If Name=\"TARGETLOCATION\" In=\"" + currentTest.testMemoryType))
                { 
                    line = sr.ReadLine();
                }
                while ((line != null) && (applicationStartAddressFound == false))
                {
                    if (line.Contains("Set Name=\"Deploy_BaseAddress\"") == true)
                    {
                        System.Diagnostics.Debug.WriteLine("line found: " + line.ToString());
                        string value = line.Split('=')[2];
                        char[] trimChars = new char[] { '\"', '/', '>' };
                        value = value.TrimStart(trimChars);
                        value = value.TrimEnd(trimChars);
                        value = value.Remove(0, 2);
                        applicationStartAddress = value;
                        applicationStartAddressFound = true;
                    } 
                    line = sr.ReadLine();
                }
                if (line == null) {
                    System.Diagnostics.Debug.WriteLine("WARNING: applicationStartAddress not found via Deploy_BaseAddress in scatterfile.  Will try to use default value.");
                }
                sr.Close();
            }
            if (applicationStartAddressFound == false)
            {
                switch (currentTest.testSolution)
                {
                    case "STM32F10x":
                        applicationStartAddress = "80A2000";
                        break;
                    case "EmoteDotNow":
                        applicationStartAddress = "80A7000";
                        break;
                    case "SOC8200":
                        applicationStartAddress = "80A2000";
                        break;
                    case "SOC_ADAPT":
                        applicationStartAddress = "805E8000";
                        break;
                    case "EmoteDotLaura":
                        applicationStartAddress = "00162000";
                        break;
                    case "Austere":
                        applicationStartAddress = "80A7000";
                        break;
                    case "STM32H743NUCLEO":
                        applicationStartAddress = "80C0000";
                        break;
                    default:
                        applicationStartAddress = "80C0000";
                        System.Diagnostics.Debug.WriteLine("WARNING: applicationStartAddress not found and no default is defined for project type " + currentTest.testSolution);
                        break;
                }
            }
            System.Diagnostics.Debug.WriteLine("applicationStartAddressFound: " + applicationStartAddressFound.ToString() + " and is: " + applicationStartAddress.ToString());

            return true;
        }


        /// <summary>
        /// Creates(default) or updates a DAT with an array of PE files.
        /// This can be used for newer projects that can't create DAT files.
        /// TODO: UNTESTED. UNTESTED. UNTESTED. UNTESTED. UNTESTED. UNTESTED.
        /// </summary>
        /// <param name="peFileNameArray">array of input pe filenames.</param>
        /// <param name="datFileName">dat output file.</param>
        /// <param name="appendExistingDAT">append files to existing DAT if true.</param>
        /// <returns>null on failure, else DAT file name.</returns>
        public string ConvertPE2DAT(string[] peFileNameArray, string datFileName = null, bool appendExistingDAT = false) {
            // create temporary manifest text file.  manifest.txt needed to use v4.3 metadataprocessor
            string manifestTempFileName = System.IO.Path.GetTempFileName();
            System.IO.File.WriteAllLines(manifestTempFileName, peFileNameArray);
            if (!appendExistingDAT) System.IO.File.Delete(datFileName);
            if (datFileName == null) datFileName = System.IO.Path.GetTempFileName() + ".dat";
            //TODO: Check that all PE files are correct version (test whether metadataprocessor looks for MSSpot1)?
            Process metaDataProcessor;
            try {
                //TODO: consider watchdog.
                metaDataProcessor = System.Diagnostics.Process.Start("MetaDataProcessor.exe", "-verbose -create_database " + manifestTempFileName + " " + datFileName);
                metaDataProcessor.WaitForExit();
                //TODO: check MetaDataProcessor.exe return codes for error conditions.
            }
            catch (System.IO.FileNotFoundException) {
                // MetaDataProcessor.exe should already be in path at C:\Program Files (x86)\Microsoft .NET Micro Framework\v4.3\Tools\MetaDataProcessor.exe
                System.Diagnostics.Debug.WriteLine("Error: install MetaDataProcessor to convert PE to DAT.");
                return null;
            }
            return datFileName;
        }


        public bool BuildNativeProject(string path, string project, TestDescription currentTest)
        {
            switch (currentTest.testSolution)
            {
                case "STM32F10x":
                    preprocessorString = "HARDWARE_EMOTE";
                    break;
                case "EmoteDotNow":
                    preprocessorString = "HARDWARE_EMOTE";
                    break;
                case "SOC8200":
                    preprocessorString = "HARDWARE_SOC8200";
                    break;
                case "SOC_ADAPT":
                    preprocessorString = "HARDWARE_ADAPT";
                    break;
                case "EmoteDotLaura":
                    preprocessorString = "HARDWARE_DOTLAURA";
                    break;
                case "Austere":
                    preprocessorString = "HARDWARE_AUSTERE";
                    break;
                case "STM32H743NUCLEO":
                    preprocessorString = "HARDWARE_H7";
                    break;
                default:
                    preprocessorString = "HARDWARE_UNKNOWN";
                    break;
            }
            ChangeDirectories(MFPath);

            string fullPath = mainHandle.textTestSourcePath;
            int index = fullPath.LastIndexOf(@"TestSuite");
            string strippedPath = path.Substring(0, index);
            RunCommand(@"SET TESTSOURCE=" + strippedPath);
            switch (usingMFVersion)
            {
                case "4.0":
                    RunCommand(@"setenv_base.cmd " + currentTest.testGCCVersion + " PORT " + mainHandle.textCompilerPath + @"\" + compilerVersionPath);
                    break;
                case "4.3":
                    RunCommand(@"setenv_gcc.cmd " + compilerVersion + " " + mainHandle.textCompilerPath + @"\" + compilerVersionPath);
                    break;
                default:
                    RunCommand(@"setenv_gcc.cmd " + compilerVersion + " " + mainHandle.textCompilerPath + @"\" + compilerVersionPath);
                    break;
            }
            ChangeDirectories(path);

            if (mainHandle.textCodeBuildSelected.Contains("Release"))
            {
                if (RunCommand(@"msbuild /maxcpucount /t:clean /p:memory=" + currentTest.testMemoryType + ",flavor=release  /p:DefineConstants=" + preprocessorString + " " + project, "Build succeeded", "Build FAILED", 1500000) != CommandStatus.Done)
                {
                    System.Diagnostics.Debug.WriteLine("native release build failed to clean.");
                    return false;
                }
                else
                    System.Diagnostics.Debug.WriteLine("native release build project cleaned.");
            }
            else
            {
                if (RunCommand(@"msbuild /maxcpucount /t:clean /p:memory=" + currentTest.testMemoryType + " /p:DefineConstants=" + preprocessorString + " " + project, "Build succeeded", "Build FAILED", 1500000) != CommandStatus.Done)
                {
                    System.Diagnostics.Debug.WriteLine("native build failed to clean.");
                    return false;
                }
                else
                    System.Diagnostics.Debug.WriteLine("native build project cleaned.");
            }
            
            if (mainHandle.textCodeBuildSelected.Contains("Release"))
            {
                if (RunCommand(@"msbuild /maxcpucount /t:build /p:memory=" + currentTest.testMemoryType + ",flavor=release  /p:DefineConstants=" + preprocessorString + " " + project, "Build succeeded", "Build FAILED", 1500000) != CommandStatus.Done)
                {
                    System.Diagnostics.Debug.WriteLine("Debug MSBuild failed to build.");
                    return false;
                }
                else
                    System.Diagnostics.Debug.WriteLine("Debug MSBuild project built.");
            }
            else
            {
                if (RunCommand(@"msbuild /maxcpucount /t:build /p:memory=" + currentTest.testMemoryType + " /p:DefineConstants=" + preprocessorString + " " + project, "Build succeeded", "Build FAILED", 1500000) != CommandStatus.Done)
                {
                    System.Diagnostics.Debug.WriteLine("Release MSBuild failed to build.");
                    return false;
                }
                else
                    System.Diagnostics.Debug.WriteLine("Release MSBuild project built.");
            }
            return true;
        }

        public bool BuildManagedProject(string path, string project, TestDescription currentTest, bool cleanBuildNeeded)
        {
            switch (currentTest.testSolution)
            {
                case "STM32F10x":
                    preprocessorString = "HARDWARE_EMOTE";
                    //applicationStartAddress = "80A2000";
                    break;
                case "EmoteDotNow":
                    preprocessorString = "HARDWARE_EMOTE";
                    //applicationStartAddress = "80A7000";
                    break;
                case "SOC8200":
                    preprocessorString = "HARDWARE_SOC8200";
                    //applicationStartAddress = "80A2000";
                    break;
                case "SOC_ADAPT":
                    preprocessorString = "HARDWARE_ADAPT";
                    //applicationStartAddress = "805E8000";
                    break;
                case "EmoteDotLaura":
                    preprocessorString = "HARDWARE_DOTLAURA";
                    break;
                case "Austere":
                    preprocessorString = "HARDWARE_AUSTERE";
                    //applicationStartAddress = "80A7000";
                    break;
                case "STM32H743NUCLEO":
                    preprocessorString = "HARDWARE_H7";
                    //applicationStartAddress = "80A7000";
                    break;
                default:
                    preprocessorString = "HARDWARE_UNKNOWN";
                    //applicationStartAddress = "80A2000";
                    break;
            }    
            ChangeDirectories(MFPath);

            string fullPath = mainHandle.textTestSourcePath;
            int index = fullPath.LastIndexOf(@"TestSuite");
            string strippedPath = path.Substring(0, index);
            RunCommand(@"SET TESTSOURCE=" + strippedPath);
            switch (usingMFVersion)
            {
                case "4.0":
                    RunCommand(@"setenv_base.cmd " + currentTest.testGCCVersion + " PORT " + mainHandle.textCompilerPath + @"\" + compilerVersionPath);
                    break;
                case "4.3":
                    RunCommand(@"setenv_gcc.cmd " + compilerVersion + " " + mainHandle.textCompilerPath + @"\" + compilerVersionPath);
                    break;
                default:
                    RunCommand(@"setenv_gcc.cmd " + compilerVersion + " " + mainHandle.textCompilerPath + @"\" + compilerVersionPath);
                    break;
            }
            ChangeDirectories(path);
            if (cleanBuildNeeded)
            {
                if (mainHandle.textCodeBuildSelected.Contains("Release"))
                {
                    if (RunCommand(@"msbuild /maxcpucount /t:clean /p:memory=" + currentTest.testMemoryType + ",flavor=release " + project, "Build succeeded", "Build FAILED", 20000) != CommandStatus.Done)
                    {
                        System.Diagnostics.Debug.WriteLine("MSBuild failed to clean.");
                        return false;
                    }
                    else
                        System.Diagnostics.Debug.WriteLine("MSBuild project cleaned.");
                }
                else
                {
                    if (RunCommand(@"msbuild /maxcpucount /t:clean /p:memory=" + currentTest.testMemoryType + " " + project, "Build succeeded", "Build FAILED", 20000) != CommandStatus.Done)
                    {
                        System.Diagnostics.Debug.WriteLine("MSBuild failed to clean.");
                        return false;
                    }
                    else
                        System.Diagnostics.Debug.WriteLine("MSBuild project cleaned.");
                }               
            }


            if (cleanBuildNeeded)
            {
                if (mainHandle.textCodeBuildSelected.Contains("Release"))
                {
                    // msbuild /t:build /p:memory=" + currentTest.testMemoryType + @" (default) (/p:memory=RAM)
                    if (RunCommand(@"msbuild /maxcpucount /t:build /p:memory=" + currentTest.testMemoryType + ",flavor=release /p:DefineConstants=" + preprocessorString + " " + project, "Build succeeded", "Build FAILED", 900000) != CommandStatus.Done)
                    {
                        System.Diagnostics.Debug.WriteLine("MSBuild failed to build.");
                        return false;
                    }
                    else
                        System.Diagnostics.Debug.WriteLine("MSBuild project built.");
                }
                else
                {
                    // msbuild /t:build /p:memory=" + currentTest.testMemoryType + @" (default) (/p:memory=RAM)
                    if (RunCommand(@"msbuild /maxcpucount /t:build /p:memory=" + currentTest.testMemoryType + " /p:DefineConstants=" + preprocessorString + " " + project, "Build succeeded", "Build FAILED", 900000) != CommandStatus.Done)
                    {
                        System.Diagnostics.Debug.WriteLine("MSBuild failed to build.");
                        return false;
                    }
                    else
                        System.Diagnostics.Debug.WriteLine("MSBuild project built.");
                }
            }

            // stripping the name of the project we are compiling to rename it to *.s19 and *_conv.s19 files
            string projectName = project;
            index = projectName.LastIndexOf('.');
            string strippedName = projectName.Substring(0, index);
            //string buildOutput = @"\BuildOutput\public\Debug\Client\dat\";
            string buildOutput = @"bin\Release\";
            if (applicationStartAddressFound == false)
            {
                switch (currentTest.testSolution)
                {
                    case "STM32F10x":
                        applicationStartAddress = "80A2000";
                        break;
                    case "EmoteDotNow":
                        applicationStartAddress = "80A7000";
                        break;
                    case "SOC8200":
                        applicationStartAddress = "80A2000";
                        break;
                    case "SOC_ADAPT":
                        applicationStartAddress = "805E8000";
                        break;
                    case "EmoteDotLaura":
                        applicationStartAddress = "00162000";
                        break;
                    case "Austere":
                        applicationStartAddress = "80A7000";
                        break;
                    case "STM32H743NUCLEO":
                        applicationStartAddress = "80C0000";
                        break;
                    default:
                        applicationStartAddress = "80A2000";
                        System.Diagnostics.Debug.WriteLine("WARNING: applicationStartAddress not found and no default is defined for project type " + currentTest.testSolution);
                        break;
                }
            }
            
            // convert to S19 record
            if (RunCommand(@"binToSrec.exe -b " + applicationStartAddress + " -i " + buildOutput + "\\le\\" + strippedName + ".dat -o " + buildOutput + strippedName + ".s19", "Conversion is Successful", "FAILED", 10000) != CommandStatus.Done)
            {
                System.Diagnostics.Debug.WriteLine("MSBuild failed to convert to S19 step 1.");
                return false;
            }
            else
                System.Diagnostics.Debug.WriteLine("MSBuild project converted to S19 step 1.");
            RunCommand(mainHandle.textCompilerPath + @"\" + compilerVersionPath + @"\bin\arm-none-eabi-objcopy.exe " + buildOutput + strippedName + @".s19 " + buildOutput + strippedName + @"_Conv.s19");
            Thread.Sleep(2000);

            return true;
        }

        public void Kill()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("MSBuildProcess killed.");
                Utility.KillProcessAndChildren(MSBuildProcess.Id);
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