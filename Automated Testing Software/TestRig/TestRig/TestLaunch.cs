using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Ports;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace TestRig
{    
    class TestLaunch
    {
        private Thread LaunchThread;
        public static Queue<TestDescription> testCollectionLaunch;
        private Mutex testCollectionMutex;
        public MainWindow mainHandle;
        public OpenOCD openOCD;
        public Git git;
        public GDB gdb;
        public TelnetBoard telnet;
        public COMPort[] COM;
        public FileTest fTest;
        public LogicAnalyzer logicTest;
        public Matlab matlab;
        public MSBuild msbuild;
        private TestReceipt testReceipt;
        public Fastboot fastboot;
        private StreamWriter sessionResult;
        private int sessionTestTotal;
        private int sessionTestComplete;
        private int sessionTestPass;
        private int currentOpenOCDInstance;
        private int currentOpenCOMInstance;
        private int maxCOMInstances = 8;

        private void process_Exited(object sender, System.EventArgs e) {
            System.Threading.Thread.Sleep(10000);

        }

        public TestLaunch(Queue<TestDescription> testCollection, Mutex collectionMutex, MainWindow passedHandle)
        {
            testCollectionLaunch = testCollection;
            testCollectionMutex = collectionMutex;

            mainHandle = passedHandle;

            openOCD = new OpenOCD();
            COM = new COMPort[maxCOMInstances];
            for (int indexCOM = 0; indexCOM < maxCOMInstances; indexCOM++)
            {
                COM[indexCOM] = new COMPort(mainHandle);
            }

            sessionTestTotal = 0;
            sessionTestComplete = 0;
            sessionTestPass = 0;

            DateTime fileTime = DateTime.Now;
            string fileName;
            fileName = fileTime.Year.ToString() + fileTime.Month.ToString("D2") + fileTime.Day.ToString("D2") + "_" + fileTime.Hour.ToString("D2") + fileTime.Minute.ToString("D2");
            //stripping first two digits of year
            fileName = fileName.Substring(2);
            string fullFileName = fileName + "_session.txt";

            // if a file already exists a number is appended to the file so as to not overwrite the file
            int num = 1;
            while (File.Exists(mainHandle.textTestReceiptPath + @"\" + fullFileName))
            {
                fullFileName = fileName + "_session" + num.ToString() + ".txt";
                num++;
            }
            sessionResult = new StreamWriter(mainHandle.textTestReceiptPath + @"\" + fullFileName);

            // starting thread to launch any locally queued up tests
            LaunchThread = new Thread(new ThreadStart(LaunchThreadFunction));
            LaunchThread.Start();
            LaunchThread.Name = "Launch Test Thread";            
        }

        public void KillLaunchThread()
        {
            // used when the program is quiting
            sessionResult.WriteLine("Tests completed: " + sessionTestComplete.ToString() + " of  " + sessionTestTotal.ToString());
            sessionResult.WriteLine("Tests passed: " + sessionTestPass.ToString() + " of  " + sessionTestTotal.ToString());
            sessionResult.Close();
            System.Diagnostics.Debug.WriteLine("Killing launch test thread.");
            if (LaunchThread != null)
            {
                if (telnet != null) telnet.Kill();
                if (gdb != null) gdb.Kill();
                if (git != null) git.Kill();
                for (int indexCOM = 0; indexCOM < maxCOMInstances; indexCOM++)
                {
                    if (COM[indexCOM].active == true)
                        COM[indexCOM].Kill();
                }                              
                if (fTest != null) fTest.Kill();
                if (openOCD.active == true) openOCD.Kill();                               
                if (matlab != null) matlab.Kill();
                if (msbuild != null) msbuild.Kill();
                if (fastboot != null) fastboot.Kill();

                if (LaunchThread.IsAlive) LaunchThread.Abort();
                LaunchThread.Join(100);
            }
        }

        private void LaunchThreadFunction()
        {
            TestDescription currentTest;
            while (true)
            {
                try
                {
                    currentTest = null;

                    // waiting for mutex to be free
                    testCollectionMutex.WaitOne();
                    // if we have tests in the test queue we will pull out the first one here
                    if (testCollectionLaunch.Count > 0)
                    {                        
                        currentTest = testCollectionLaunch.Dequeue();                        
                    }
                    testCollectionMutex.ReleaseMutex();

                    // launching a test if there is one
                    if (currentTest != null)
                    {
                        testReceipt = new TestReceipt(currentTest);
                        DateTime startTime = DateTime.Now;

                        string returnReason = ExecuteTest(currentTest);
                        //string returnReason="";   // for debugging
                        //Thread.Sleep(5000);       // for debugging

                        DateTime stopTime = DateTime.Now;
                        TimeSpan duration = stopTime - startTime;
                        testReceipt.testDuration = duration;

                        testReceipt.testExecutionMachine = System.Environment.MachineName.ToString();

                        if (returnReason != "Test completed")
                        {
                            // test failed, killing anything not already torn down here
                            if (msbuild != null) msbuild.Kill();
                            if (git != null) git.Kill();
                            if (telnet != null) telnet.Kill();
                            for (int indexCOM = 0; indexCOM < maxCOMInstances; indexCOM++)
                            {
                                if (COM[indexCOM].active == true)
                                    COM[indexCOM].Kill();
                            } 
                            if (fTest != null) fTest.Kill();                            
                            if (gdb != null) gdb.Kill();
                            if (openOCD.active == true) openOCD.Kill();
                            if (fastboot != null) fastboot.Kill();
                            
                            System.Diagnostics.Debug.WriteLine("Test FAILED because of:" + returnReason);
                        }

                        testReceipt.testResult = returnReason;

                        string TRPath = mainHandle.textTestReceiptPath;

                        // we won't write test receipts for support projects (contains the words "support project" but not the word "load")
                        if ((currentTest.testSupporting.Contains("load") == true) || (currentTest.testSupporting.Contains("support project") == false))
                        {
                            testReceipt.WriteFile(TRPath);
                            sessionTestTotal++;
                            if (testReceipt.testPass == true)
                                sessionTestPass++;
                            if (testReceipt.testComplete == true)
                                sessionTestComplete++;
                        }

                        // The test is over so we will pull it from the "Test Status" tab display
                        mainHandle.Dispatcher.BeginInvoke(mainHandle.removeDelegate);
                    }                
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Launch Thread FAIL: " + ex.Message);
                }
            }
        }

        public string DebugFunction(){

            /*openOCD = new OpenOCD(mainHandle, currentTest);
            if (openOCD == null) return "OpenOCD failed to load";
            gdb = new GDB(mainHandle);
            if (gdb == null) return "GDB failed to load";
            telnet = new TelnetBoard(mainHandle);
            if (telnet == null) return "Telnet failed to load";

            if (telnet.Start() == false) return "Telnet failed to start";
            if (telnet.Clear() == false) return "Telnet failed to clear FLASH";                    

                        
                    //if (gdb.Load(MFPath + @"\" + @"BuildOutput\THUMB2\" + currentTest.testGCCVersion + @"\le\" + currentTest.testMemoryType + @"\debug\" + currentTest.testSolution + @"\bin\" + currentTest.testSolutionType + ".axf") == false) return "GDB failed to load MF AXF file";
                        


                //if (telnet.Load(workingDirectory + @"\" + buildOutput + strippedName + "_Conv.s19") == false) return "Telnet failed to load";
                   
                    
            if (gdb.Continue() == false) return "GDB failed to start processor";

            if (msbuild != null) msbuild.Kill();
            if (git != null) git.Kill();
            if (telnet != null) telnet.Kill();
            ifor (int indexCOM = 0; indexCOM < maxCOMInstances; indexCOM++)
        {
            if (COM[indexCOM].active == true)
                        COM[indexCOM].Kill();
        } 
            if (fTest != null) fTest.Kill();            
            if (gdb != null) gdb.Kill();
            if (openOCD != null) openOCD.Kill();
            if (fastboot != null) fastboot.Kill();
            */
            return null;
        }



        private string ExecuteTest(TestDescription currentTest)
        {
            try
            {                
                string workingDirectory = mainHandle.textTestSourcePath + @"\" + currentTest.testPath;
                string projectName = currentTest.buildProj;
                int index = projectName.LastIndexOf('.');
                string strippedName = projectName.Substring(0, index);
                string MFPath;
                string testDataName = String.Empty;
                ProcessStartInfo TestExecutableInfo = new ProcessStartInfo();
                ProcessStartInfo TestAnalysisExecutableInfo = new ProcessStartInfo();
                Process TestExecutableProcess = new Process();
                Process TestAnalysisExecutableProcess = new Process();
                DateTime testStartTime = DateTime.Now;
                int indexDevice;
                int numberOfCodeLoads = 1;

                switch (currentTest.testMFVersionNum)
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
                
                #region Retrieving code
                currentTest.testState = "Retrieving code";
                mainHandle.Dispatcher.BeginInvoke(mainHandle.updateDelegate);
                
                git = new Git(mainHandle);
                if (git == null) return "Git failed to load";

                // Checkout code from GitHub if needed
                if (currentTest.testGitOption.Equals("Use local code"))
                {
                    // we do not checkout code if we are using the local copies
                    System.Diagnostics.Debug.WriteLine("Using local code for test.");
                }
                else if (currentTest.testGitOption.Equals("Use archive code"))
                {
                    System.Diagnostics.Debug.WriteLine("Checking out archived code.");
                    if (git.CloneCode() == false) return "Git failed to Clone";
                }
                else if (currentTest.testGitOption.Equals("Use archive branch code"))
                {
                    System.Diagnostics.Debug.WriteLine("Checking out branch <" + currentTest.testGitBranch + "> of archived code.");
                    if (git.CloneCodeBranch(currentTest.testGitBranch) == false) return "Git failed to clone branch: " + currentTest.testGitBranch;
                }
                
                git.Kill();    
                #endregion
                
                #region Building code
                System.Diagnostics.Debug.WriteLine("Building code");
                msbuild = new MSBuild(mainHandle, currentTest.testMFVersionNum);
                if (msbuild == null) return "MSBuild failed to load";

                if (currentTest.testType == "C#")
                {
                    if (currentTest.testUsePrecompiledBinary == String.Empty)
                    {
						if (currentTest.testSolution.Equals("SOC_ADAPT")) {
                            // TODO build littlekernel or retrieve it.
                        }
                        else {
							currentTest.testState = "Building TinyBooter";
							mainHandle.Dispatcher.BeginInvoke(mainHandle.updateDelegate);
							currentTest.testSolutionType = "TinyBooter";
							if (msbuild.BuildTinyCLR(currentTest) == false) return "MSBuild failed to build TinyBooter";
						}
                        currentTest.testState = "Building TinyCLR";
                        mainHandle.Dispatcher.BeginInvoke(mainHandle.updateDelegate);
                        currentTest.testSolutionType = "TinyCLR";
                        if (msbuild.BuildTinyCLR(currentTest) == false) return "MSBuild failed to build TinyCLR";
                    }
                    currentTest.testState = "Building managed code";
                    mainHandle.Dispatcher.BeginInvoke(mainHandle.updateDelegate);
                    if (msbuild.BuildManagedProject(workingDirectory, currentTest.buildProj, currentTest) == false) return "MSBuild failed to build managed project";
                }
                else
                {
                    if (currentTest.testSolution.Equals("SOC_ADAPT"))
                    {
                        // TODO build littlekernel or retrieve it.
                    }
                    else
                    {
                        currentTest.testState = "Building TinyBooter";
                        mainHandle.Dispatcher.BeginInvoke(mainHandle.updateDelegate);
                        currentTest.testSolutionType = "TinyBooter";
                        if (msbuild.BuildTinyCLR(currentTest) == false) return "MSBuild failed to build TinyBooter";
                    }
                    currentTest.testState = "Building native code";
                    mainHandle.Dispatcher.BeginInvoke(mainHandle.updateDelegate);
                    currentTest.testSolutionType = "TinyCLR";
                    if (msbuild.BuildNativeProject(workingDirectory, currentTest.buildProj, currentTest) == false) return "MSBuild failed to build native project";
                }

                msbuild.Kill();
                #endregion       
                
                #region Reading parameters
                // Preparing data gathering systems and analysis
                // Grabbing data for sample time length                
                ReadParameters readVars;

                if (currentTest.testType == "C#")
                    readVars = new ReadParameters(workingDirectory + @"\" + "Parameters.cs", currentTest);
                else
                    readVars = new ReadParameters(workingDirectory + @"\" + "Parameters.h", currentTest);

                System.Diagnostics.Debug.WriteLine("testTimeout: " + currentTest.testTimeout.ToString());
                System.Diagnostics.Debug.WriteLine("useLogic: " + currentTest.testUseLogic);
                System.Diagnostics.Debug.WriteLine("sampleTimeMs: " + currentTest.testSampleTimeMs.ToString());
                System.Diagnostics.Debug.WriteLine("sampleFrequency: " + currentTest.testSampleFrequency.ToString());
                System.Diagnostics.Debug.WriteLine("useTestScript: " + currentTest.testUseScript.ToString());
                System.Diagnostics.Debug.WriteLine("testScriptName: " + currentTest.testScriptName.ToString());
                System.Diagnostics.Debug.WriteLine("testScriptTimeoutMs: " + currentTest.testScriptTimeoutMs.ToString());
                System.Diagnostics.Debug.WriteLine("useCOM: " + currentTest.testUseCOM.ToString());
                System.Diagnostics.Debug.WriteLine("forceCOM: " + currentTest.testForceCOM.ToString());
                System.Diagnostics.Debug.WriteLine("COMParameters: " + currentTest.testCOMParameters.ToString());
                System.Diagnostics.Debug.WriteLine("useAnalysis: " + currentTest.testAnalysis.ToString());
                System.Diagnostics.Debug.WriteLine("analysisScriptName: " + currentTest.testAnalysisScriptName.ToString());
                System.Diagnostics.Debug.WriteLine("useResultsFile: " + currentTest.testUseResultsFile.ToString());
                System.Diagnostics.Debug.WriteLine("resultsFileName: " + currentTest.testResultsFileName.ToString());
                #endregion

                #region Loading the current test
                //string buildOutput = @"\BuildOutput\public\Debug\Client\dat\";
                string buildOutput = @"bin\Release\";

                if (currentTest.testSolution.Equals("SOC_ADAPT"))
                {                    
                    if(currentTest.testJTAGHarness.Equals("Lauterbach")) {
                        System.Diagnostics.Debug.WriteLine("Executing ADAPT JTAG test for:  " + currentTest.testName);

                        string fnameOS = MFPath + @"\" + @"BuildOutput\ARM\" + currentTest.testGCCVersion + @"\le\" + currentTest.testMemoryType + @"\debug\" + currentTest.testSolution + @"\bin\" + currentTest.testSolutionType + @".axf";
                        // Lauterbach can use s19.
                        string fnameMSIL = workingDirectory + @"\" + buildOutput + strippedName + "_Conv.s19";

                        if (!System.IO.File.Exists(fnameMSIL)) {
                            Debug.WriteLine("ERROR: DAT doesn't exist.");
                            //TODO: get PE file list from user's test config file.
                            //string ret = ConvertPE2DAT(new string[]{workingDirectory + @"\" + buildOutput + strippedName + ".pe"}, fnameMSIL);
                            //if (ret == null) Debug.WriteLine("ERROR: couldn't create DAT file " + fnameMSIL);
                        }

                        //TODO: axf vs ER_FLASH?
                        currentTest.testState = "Loading MSIL code and OS code via Lauterbach to Adapt";
                        //currentTest.testState = "Loading OS code via Lauterbach to Adapt";

                        //current plan: just call powershell, then later use library wrapper.  
                        //string fnameMSIL = @"C:\repo\DotNet-MF\MicroFrameworkPK_v4_3\BuildOutput\ARM\GCC4.2\le\RAM\debug\SOC_ADAPT\bin\tinyclr.axf";
                        //string fnameOS = @"C:\Executables\HelloThere_Conv.s19";
                        //TODO: make T32 directories user-configurable
                        string t32configscript = @"C:\Users\researcher\T32Debug_20120824\T32Debug";
                        Process t32shell = new Process();
                        t32shell.StartInfo.FileName = "powershell.exe";
                        t32shell.StartInfo.Arguments = " -executionpolicy unrestricted \"\"" + t32configscript + "\\start_t32.ps1\"\"  \'" + fnameMSIL + "\' \'" + fnameOS + "\'";
                        t32shell.StartInfo.WorkingDirectory = t32configscript;
                        t32shell.StartInfo.UseShellExecute = false;
                        t32shell.Start();
                        
                        t32shell.WaitForExit();
                       
                    }
                    else 
                    {
					    System.Diagnostics.Debug.WriteLine("Executing ADAPT fastboot test for:  " + currentTest.testName);
                        fastboot = new Fastboot(mainHandle, currentTest.testPowerAutomateSelected);
                        currentTest.testState = "Entering Fastboot mode";
                        mainHandle.Dispatcher.BeginInvoke(mainHandle.updateDelegate);
                        if (fastboot == null) return "Fastboot failed to load";
                        if (fastboot.enterFastbootMode() == false) return "Failed to enter Fastboot mode";

                        if (currentTest.testType == "C#")
                        {
                            //string fnameOS = "D:/Test/pad_test/f1.bin";
                            string inFile1Name = MFPath + @"\" + @"BuildOutput\ARM\" + currentTest.testGCCVersion + @"\le\" + currentTest.testMemoryType + @"\debug\" + currentTest.testSolution + @"\bin\" + currentTest.testSolutionType + @".bin";
                            //string fnameMSIL = "D:/Test/pad_test/f2.bin";
                            //string fnameMSIL = workingDirectory + @"\" + buildOutput + strippedName + "_Conv.s19";
                            // Adapt uses .dat file instead of converted s19
                            string inFile2Name = workingDirectory + @"\" + buildOutput + strippedName + ".dat";
                            string concatFileName = workingDirectory + @"\" + "MF_managed.bin";                            
                            if (fastboot.createFinalBinary(inFile1Name, inFile2Name, concatFileName) == false) return "Failed to create final Adapt binary";
                            currentTest.testState = "Loading via fastboot to Adapt";
                            mainHandle.Dispatcher.BeginInvoke(mainHandle.updateDelegate);
                            if (fastboot.load(concatFileName) == false) return "Failed to load Adapt binary";
                        }
                        else
                        {
                            string fileName = MFPath + @"\" + @"BuildOutput\ARM\" + currentTest.testGCCVersion + @"\le\" + currentTest.testMemoryType + @"\debug\" + currentTest.testSolution + @"\bin\" + strippedName + @".bin"; 
                            currentTest.testState = "Loading via fastboot to Adapt";
                            mainHandle.Dispatcher.BeginInvoke(mainHandle.updateDelegate);
                            currentTest.testState = "Loading via fastboot to Adapt";
                            mainHandle.Dispatcher.BeginInvoke(mainHandle.updateDelegate);
                            if (fastboot.load(fileName) == false) return "Failed to load Adapt binary";
                        }
					    if (fastboot.run() == false) return "Failed to run Adapt binary";
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Executing test for:  " + currentTest.testName);
                    
                    // some tests will load code to multiple devices (load  indentical) and others will load tests to support devices (support project)
                    if (currentTest.testSupporting.StartsWith("support project"))
                    {
                        // this support project will load code to the secondary device number listed in the test description (i.e. "support project 1" will be programmed to the support device 1)
                        string tempString = currentTest.testSupporting;
                        tempString = tempString.Trim();
                        tempString = currentTest.testSupporting.Remove(0, 15);
                        tempString = tempString.Trim();
                        int instanceNum = int.Parse(tempString);

                        numberOfCodeLoads = 1;
                        currentOpenOCDInstance = instanceNum;
                        currentOpenCOMInstance = instanceNum;
                        System.Diagnostics.Debug.WriteLine("Support project: " + numberOfCodeLoads.ToString() + " instance: " + currentOpenOCDInstance.ToString());
                    }
                    else if (currentTest.testSupporting.StartsWith("load indentical"))
                    {                        
                        // this project will get loaded to the primary and secondary devices (i.e. "load identical 3" will be loaded wo the primary and two secondary devices)
                        string tempString = currentTest.testSupporting;
                        tempString = tempString.Trim();
                        tempString = currentTest.testSupporting.Remove(0, 16);
                        tempString = tempString.Trim();
                        int supportNum = int.Parse(tempString);

                        numberOfCodeLoads = supportNum;
                        currentOpenOCDInstance = 0;
                        currentOpenCOMInstance = 0;
                        System.Diagnostics.Debug.WriteLine("Load identical project: " + numberOfCodeLoads.ToString() + " instance: " + currentOpenOCDInstance.ToString());
                    }
                    else
                    {
                        // this project will only be loaded onto the primary device
                        numberOfCodeLoads = 1;
                        currentOpenOCDInstance = 0;
                        currentOpenCOMInstance = 0;
                        System.Diagnostics.Debug.WriteLine("Normal project: " + numberOfCodeLoads.ToString() + " instance: " + currentOpenOCDInstance.ToString());
                    }

                    for (indexDevice = 0; indexDevice < numberOfCodeLoads; indexDevice++)
                    {
                        System.Diagnostics.Debug.WriteLine("Code load number " + indexDevice.ToString() + " instance: " + currentOpenOCDInstance.ToString());
                        openOCD.Connect(mainHandle, currentOpenOCDInstance);
                        if (openOCD.active == false) return "OpenOCD failed to load";
                        gdb = new GDB(mainHandle);
                        if (gdb == null) return "GDB failed to load";
                        telnet = new TelnetBoard(mainHandle);
                        if (telnet == null) return "Telnet failed to load";

                        if (telnet.Start() == false) return "Telnet failed to start";
                        if (telnet.Clear() == false) return "Telnet failed to clear FLASH";

                        if (currentTest.testType == "C#")
                        {
                            if (currentTest.testUsePrecompiledBinary != String.Empty)
                            {
                                currentTest.testState = "Loading MF AXF";
                                mainHandle.Dispatcher.BeginInvoke(mainHandle.updateDelegate);
                                if (gdb.Load(workingDirectory + @"\" + currentTest.testUsePrecompiledBinary) == false) return "GDB failed to load precompiled MF AXF file: " + currentTest.testUsePrecompiledBinary;
                            }
                            else
                            {
                                currentTest.testState = "Loading TinyBooter";
                                mainHandle.Dispatcher.BeginInvoke(mainHandle.updateDelegate);
                                currentTest.testSolutionType = "TinyBooter";
                                if (gdb.Load(MFPath + @"\" + @"BuildOutput\THUMB2\" + currentTest.testGCCVersion + @"\le\" + currentTest.testMemoryType + @"\debug\" + currentTest.testSolution + @"\bin\" + currentTest.testSolutionType + ".axf") == false) return "GDB failed to load MF AXF file";
                                currentTest.testState = "Loading TinyCLR";
                                mainHandle.Dispatcher.BeginInvoke(mainHandle.updateDelegate);
                                currentTest.testSolutionType = "TinyCLR";
                                if (gdb.Load(MFPath + @"\" + @"BuildOutput\THUMB2\" + currentTest.testGCCVersion + @"\le\" + currentTest.testMemoryType + @"\debug\" + currentTest.testSolution + @"\bin\" + currentTest.testSolutionType + ".axf") == false) return "GDB failed to load MF AXF file";
                            }

                            currentTest.testState = "Loading managed code";
                            mainHandle.Dispatcher.BeginInvoke(mainHandle.updateDelegate);

                            if (telnet.Load(workingDirectory + @"\" + buildOutput + strippedName + "_Conv.s19") == false) return "Telnet failed to load";
                        }
                        else
                        {
                            if (currentTest.testUsePrecompiledBinary != String.Empty)
                            {
                                currentTest.testState = "Loading test AXF";
                                mainHandle.Dispatcher.BeginInvoke(mainHandle.updateDelegate);
                                if (gdb.Load(workingDirectory + @"\" + currentTest.testUsePrecompiledBinary) == false) return "GDB failed to load precompiled AXF file: " + currentTest.testUsePrecompiledBinary;
                            }
                            else
                            {
                                currentTest.testState = "Loading native code";
                                mainHandle.Dispatcher.BeginInvoke(mainHandle.updateDelegate);
                                currentTest.testSolutionType = "TinyCLR";
                                if (gdb.Load(MFPath + @"\" + @"BuildOutput\THUMB2\" + currentTest.testGCCVersion + @"\le\" + currentTest.testMemoryType + @"\debug\" + currentTest.testSolution + @"\bin" + @"\" + strippedName + ".axf") == false) return "GDB failed to load compiled AXF";
                            }
                        }

                        currentTest.testState = "Starting processor";
                        mainHandle.Dispatcher.BeginInvoke(mainHandle.updateDelegate);
                        if (currentTest.testUseCOM == true)
                        {
                            for (indexDevice = 0; indexDevice < numberOfCodeLoads; indexDevice++)
                            {
                                if (COM[currentOpenCOMInstance].Connect(currentTest, testReceipt, currentOpenCOMInstance) == false) return "COM " + indexDevice.ToString() + " failed to open";
                            }
                        }
                        if (gdb.Continue() == false) return "GDB failed to start processor";
                        telnet.Kill();
                        gdb.Kill();
                        openOCD.Kill();                        
                        currentOpenOCDInstance++;
                    }
                }
                #endregion

                #region Getting ready to run test
                /*if (currentTest.testUseCOM == true)
                {
                    for (indexDevice = 0; indexDevice < numberOfCodeLoads; indexDevice++)
                    {
                        if (COM[currentOpenCOMInstance].Connect(currentTest, testReceipt, currentOpenCOMInstance) == false) return "COM " + indexDevice.ToString() + " failed to open";
                    }
                }*/
                if (Directory.Exists(workingDirectory + @"\" + "testTemp")) Directory.Delete(workingDirectory + @"\" + "testTemp", true);
                Directory.CreateDirectory(workingDirectory + @"\" + "testTemp");
                if ((currentTest.testUseLogic.Equals("normal") == true) || (currentTest.testUseLogic.Equals("I2C") == true) || (currentTest.testUseLogic.Equals("i2c") == true))
                {
                    if (logicTest == null)
                        logicTest = new LogicAnalyzer(currentTest.testSampleFrequency, workingDirectory + @"\" + strippedName + ".hkp");
                    else
                        logicTest.Initialize(currentTest.testSampleFrequency, workingDirectory + @"\" + strippedName + ".hkp");
                    if (logicTest == null) return "Logic Analyzer failed to load";
                    if ((currentTest.testUseLogic.Equals("I2C") == true) || (currentTest.testUseLogic.Equals("i2c") == true))
                    {
                        testDataName = "testData.txt";
                    }
                    else
                    {
                        testDataName = "testData.csv";
                    }
                    if (logicTest.startMeasure(workingDirectory + @"\testTemp\" + testDataName, currentTest.testSampleTimeMs, currentTest.testUseLogic) == false) return "Logic Analyzer failed to start measuring";
                }

                // starting to measure time of test 
                testStartTime = DateTime.Now;

                if (currentTest.testUseScript == true)
                {
                    TestExecutableInfo = new ProcessStartInfo();
                    TestExecutableProcess = new Process();

                    System.Diagnostics.Debug.WriteLine("Starting to run test command prompt: " + currentTest.testScriptName);

                    TestExecutableInfo.CreateNoWindow = true;
                    TestExecutableInfo.RedirectStandardInput = false;
                    TestExecutableInfo.UseShellExecute = false;
                    //TestExecutableInfo.FileName = @"cmd.exe";

                    TestExecutableProcess.StartInfo = TestExecutableInfo;
                    //TestExecutableProcess.Start();
                    //input = TestExecutableProcess.StandardInput;
                }
                #endregion

                #region Running test
                currentTest.testState = "Running test";
                mainHandle.Dispatcher.BeginInvoke(mainHandle.updateDelegate);                
                if (currentTest.testUseScript == true)
                {
                    System.Diagnostics.Debug.WriteLine("Waiting 5 seconds for DUT to startup and flush serial buffer");
                    Thread.Sleep(5000);

                    // if there is a COM port already open we will close it now so we can start a new COM port with possibly different settings
                    for (int indexCOM = 0; indexCOM < maxCOMInstances; indexCOM++)
                    {
                        if (COM[indexCOM].active == true)
                            COM[indexCOM].Kill();
                    }
                    Thread.Sleep(6000);
                    for (indexDevice = 0; indexDevice < numberOfCodeLoads; indexDevice++)
                    {
                        if (COM[currentOpenCOMInstance].Connect(currentTest, testReceipt, currentOpenCOMInstance) == false) return "COM " + indexDevice.ToString() + " failed to open";
                    }

                    fTest = new FileTest(mainHandle, currentTest);
                    if (fTest == null) return "FileTest failed to open";

                    StreamReader script;
                    String line;
                    string[] parsedLine;
                    int runTime = 0;

                    if (File.Exists(workingDirectory + @"\" + currentTest.testScriptName) == false) return "Specified script: " + currentTest.testScriptName + " does not exist.";                    
                    System.Diagnostics.Debug.WriteLine("Running test script: " + currentTest.testScriptName);
                    script = new StreamReader(workingDirectory + @"\" + currentTest.testScriptName);
                    line = script.ReadLine();
                    while (line != null)
                    {
                        System.Diagnostics.Debug.WriteLine("Running script line: " + line);
                        parsedLine = line.Split(' ');
                        if (line.StartsWith("#"))
                        {
                            System.Diagnostics.Debug.WriteLine("Script comment: " + line);
                        }
                        else if (line.StartsWith("execute"))
                        {                            
                            try
                            {
                                runTime = int.Parse(parsedLine[2].Trim());
                                // see what is to be exectued (exe, dll, other)
                                if (parsedLine[1].EndsWith(".exe"))
                                {
                                    TestExecutableInfo.FileName = parsedLine[1].Trim();                                    

                                    TestExecutableProcess.Start();
                                    TestExecutableProcess.WaitForExit(runTime);
                                    if (TestExecutableProcess.HasExited == false)
                                        TestExecutableProcess.Kill();
                                }
                                else if (parsedLine[1].EndsWith(".dll"))
                                {
                                }
                                else if (parsedLine[1].EndsWith(".ps1"))
                                {
                                    PowerShell powerShell = PowerShell.Create();

                                    powerShell.AddScript(File.ReadAllText(workingDirectory + @"\" + "Test.ps1"));
                                    var results = powerShell.Invoke();
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine("failed to execute: " + parsedLine[1].Trim() + " running for (ms): " + runTime.ToString() + ": " + ex.Message);
                                return "failed to execute: " + parsedLine[1].Trim() + " running for (ms): " + runTime.ToString();
                            }
                        }
                        else if (line.StartsWith("file"))
                        {
                            if (parsedLine[1].Contains("random"))
                            {
                                Random random;
                                int randomNumber;

                                if (parsedLine[2].Contains("none"))
                                    random = new Random();
                                else
                                    random = new Random(int.Parse(parsedLine[2].Trim()));

                                int byteNum = int.Parse(parsedLine[3].Trim());
                                int lowerBound = int.Parse(parsedLine[4].Trim());
                                int upperBound = int.Parse(parsedLine[5].Trim());

                                System.Diagnostics.Debug.WriteLine("Sending " + byteNum.ToString() + " random bytes (" + lowerBound.ToString() + "," + upperBound.ToString() + ") to COM port");

                                for (int i = 0; i < byteNum; i++)
                                {
                                    randomNumber = random.Next(lowerBound, upperBound);
                                    fTest.Save(randomNumber.ToString() + "\r\n");
                                }
                            }
                            else if (parsedLine[1].Contains("file"))
                            {
                                string fileName = parsedLine[2].Trim();
                                if (File.Exists(workingDirectory + @"\" + fileName) == false) return "Specified fTest file: " + fileName + " does not exist.";
                                System.Diagnostics.Debug.WriteLine("Sending data file: " + workingDirectory + @"\" + fileName + " to COM port");
                                fTest.SendFile(workingDirectory + @"\" + fileName);
                            }
                            else if (parsedLine[1].Contains("string"))
                            {
                                int location = line.IndexOf("string ");
                                fTest.Save(line.Remove(0, location + 7));
                            }
                            else if (parsedLine[1].Contains("enable") || parsedLine[1].Contains("disable"))
                            {
                                string fileName = parsedLine[2].Trim();
                                if (fTest.SaveToFile(parsedLine[1].Trim(), workingDirectory + @"\" + fileName) == false) return "Specified test file: " + workingDirectory + @"\" + fileName + " could not be opened.";
                            }                            
                        }
                        else if (line.StartsWith("COM_send"))
                        {
                            if (parsedLine[1].Contains("random"))
                            {
                                Random random;
                                int randomNumber;

                                if (parsedLine[2].Contains("none"))
                                    random = new Random();
                                else
                                    random = new Random(int.Parse(parsedLine[2].Trim()));

                                int byteNum = int.Parse(parsedLine[3].Trim());
                                int lowerBound = int.Parse(parsedLine[4].Trim());
                                int upperBound = int.Parse(parsedLine[5].Trim());

                                System.Diagnostics.Debug.WriteLine("Sending " + byteNum.ToString() + " random bytes (" + lowerBound.ToString() + "," + upperBound.ToString() + ") to COM port");

                                for (int i = 0; i < byteNum; i++)
                                {
                                    randomNumber = random.Next(lowerBound, upperBound);
                                    COM[currentOpenCOMInstance].Send(randomNumber.ToString() + "\r\n");
                                    // we have to throttle sending data for now or the eMote COM receive breaks
									Thread.Sleep(100);
                                }
                            }
                            else if (parsedLine[1].Contains("file"))
                            {
                                string fileName = parsedLine[2].Trim();
                                if (File.Exists(workingDirectory + @"\" + fileName) == false) return "Specified COM_send file: " + fileName + " does not exist.";
                                System.Diagnostics.Debug.WriteLine("Sending data file: " + workingDirectory + @"\" + fileName + " to COM port");
                                COM[currentOpenCOMInstance].SendFile(workingDirectory + @"\" + fileName);
                            }
                            else if (parsedLine[1].Contains("string"))
                            {
                                int location = line.IndexOf("string ");
                                COM[currentOpenCOMInstance].Send(line.Remove(0, location + 7));
                            }
                        }
                        else if (line.StartsWith("COM_receive"))
                        {
                            if (parsedLine[1].Contains("file"))
                            {
                                string fileName = parsedLine[3].Trim();
                                if (COM[currentOpenCOMInstance].SaveToFile(parsedLine[2].Trim(), workingDirectory + @"\" + fileName) == false) return "Specified COM_receive file: " + workingDirectory + @"\" + fileName + " could not be opened.";
                            }
                        }
                        else if (line.StartsWith("sleep"))
                        {
                            int waitTimeMs = int.Parse(parsedLine[1].Trim());

                            System.Diagnostics.Debug.WriteLine("Script sleeping for " + waitTimeMs.ToString() + " ms.");
                            Thread.Sleep(waitTimeMs);
                        }
                        else if (line.StartsWith("test_result"))
                        {
                            if ((parsedLine[1].Contains("file")) && (parsedLine[2].Contains("compare")))
                            {
                                string fileName1 = parsedLine[3].Trim();
                                string fileName2 = parsedLine[4].Trim();
                                testReceipt.testPass = fTest.Compare(workingDirectory + @"\" + fileName1, workingDirectory + @"\" + fileName2);
                                testReceipt.testComplete = true;                                
                            }
                            else if (parsedLine[1].Contains("results"))
                            {
                                try
                                {
                                    StreamReader tResult = new StreamReader(workingDirectory + @"\" + parsedLine[2]);
                                    string resultLine;

                                    resultLine = tResult.ReadLine();
                                    while (resultLine != null)
                                    {
                                        if (resultLine.Contains("result ="))
                                        {
                                            if (resultLine.Contains("PASS"))
                                                testReceipt.testPass = true;
                                            else
                                                testReceipt.testPass = false;
                                        }
                                        else if (resultLine.Contains("accuracy"))
                                        {
                                            index = resultLine.IndexOf('=') + 2;
                                            testReceipt.testAccuracy = double.Parse(resultLine.Substring(index, resultLine.Length - index));
                                        }
                                        else if (resultLine.Contains("resultParameter1"))
                                        {
                                            index = resultLine.IndexOf('=') + 2;
                                            testReceipt.testReturnParameter1 = resultLine.Substring(index, resultLine.Length - index);
                                        }
                                        else if (resultLine.Contains("resultParameter2"))
                                        {
                                            index = resultLine.IndexOf('=') + 2;
                                            testReceipt.testReturnParameter2 = resultLine.Substring(index, resultLine.Length - index);
                                        }
                                        else if (resultLine.Contains("resultParameter3"))
                                        {
                                            index = resultLine.IndexOf('=') + 2;
                                            testReceipt.testReturnParameter3 = resultLine.Substring(index, resultLine.Length - index);
                                        }
                                        else if (resultLine.Contains("resultParameter4"))
                                        {
                                            index = resultLine.IndexOf('=') + 2;
                                            testReceipt.testReturnParameter4 = resultLine.Substring(index, resultLine.Length - index);
                                        }
                                        else if (resultLine.Contains("resultParameter5"))
                                        {
                                            index = resultLine.IndexOf('=') + 2;
                                            testReceipt.testReturnParameter5 = resultLine.Substring(index, resultLine.Length - index);
                                            testReceipt.testComplete = true;
                                        }
                                        resultLine = tResult.ReadLine();
                                    }
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine("test results read FAIL: " + ex.Message);
                                    return "Test result file read failure: " + ex.Message;
                                }
                            }
                        }
                        line = script.ReadLine();
                    }
                    script.Close();
                }
                #endregion

                #region Stop data capture if occurring 
                DateTime testStopTime = DateTime.Now;
                TimeSpan duration = testStopTime - testStartTime;

                if ((currentTest.testUseLogic.Equals("normal") == true) || (currentTest.testUseLogic.Equals("I2C") == true) || (currentTest.testUseLogic.Equals("i2c") == true))
                {                           
                    if (logicTest == null) return "Logic Analyzer failed to load";
                    // shutting down logic analyzer if we sampled long enough
                    if ((int)duration.TotalMilliseconds < currentTest.testSampleTimeMs)
                    {
                        System.Diagnostics.Debug.WriteLine("Waiting for end of logic sampling to end (" + (currentTest.testSampleTimeMs - (int)duration.TotalMilliseconds).ToString() + ") ms");
                        Thread.Sleep(currentTest.testSampleTimeMs - (int)duration.TotalMilliseconds);
                    }
                    if (logicTest.stopMeasure() == false) return "Logic Analyzer failed to stop measuring";
                }
                
                testStopTime = DateTime.Now;
                duration = testStopTime - testStartTime;
                int remainingCOMtimeMs = currentTest.testTimeout - (int)duration.TotalMilliseconds;
                int tenthWaitTimeMs = remainingCOMtimeMs / 10;

                System.Diagnostics.Debug.WriteLine("Test timeout (" + (remainingCOMtimeMs).ToString() + ") ms");
                while ((currentTest.testUseCOM == true) && (testReceipt.testComplete != true) && (remainingCOMtimeMs > 0) )
                {
                    System.Diagnostics.Debug.WriteLine("Waiting for test to timeout (" + (remainingCOMtimeMs).ToString() + ") ms");
                    // if we are using the COM port then we will wait for the test to complete before moving on
                    Thread.Sleep(tenthWaitTimeMs);
                    testStopTime = DateTime.Now;
                    duration = testStopTime - testStartTime;
                    remainingCOMtimeMs = currentTest.testTimeout - (int)duration.TotalMilliseconds;
                }
                #endregion
                for (int indexCOM = 0; indexCOM < maxCOMInstances; indexCOM++)
                {
                    if (COM[indexCOM].active == true)
                        COM[indexCOM].Kill();
                } 
                if (telnet != null) telnet.Kill();
                if (gdb != null) gdb.Kill();
                if (openOCD.active == true) openOCD.Kill();
                if (fastboot != null) fastboot.Kill();

                #region Analyzing test
                currentTest.testState = "Analyzing test";
                mainHandle.Dispatcher.BeginInvoke(mainHandle.updateDelegate);
                
                Thread.Sleep(50);

                testStopTime = DateTime.Now;
                duration = testStopTime - testStartTime;
                int analysisTimeout = currentTest.testTimeout - (int)duration.TotalMilliseconds;
                // if the timeout already occurred we should still run the analysis for at least 10 seconds.
                if (analysisTimeout < 10000)
                    analysisTimeout = 10000;
                if ((currentTest.testAnalysis.Equals("matlab") == true) || (currentTest.testAnalysis.Equals("Matlab") == true))
                {
                    matlab = new Matlab(mainHandle, testReceipt);
                    if (matlab == null) return "Matlab failed to load";
                    if (matlab.matlabRunScript(workingDirectory, @"testTemp\" + testDataName, currentTest) == false) return "Matlab failed to run script";
                }
                else if (currentTest.testAnalysis.Equals("exe") == true)
                {
                    TestAnalysisExecutableInfo = new ProcessStartInfo();
                    TestAnalysisExecutableProcess = new Process();

                    System.Diagnostics.Debug.WriteLine("Starting to run analysis executable: " + currentTest.testAnalysisScriptName);

                    TestAnalysisExecutableInfo.CreateNoWindow = true;
                    TestAnalysisExecutableInfo.RedirectStandardInput = false;
                    TestAnalysisExecutableInfo.UseShellExecute = false;

                    TestAnalysisExecutableProcess.StartInfo = TestAnalysisExecutableInfo;
                    TestAnalysisExecutableInfo.FileName = workingDirectory + @"\" + currentTest.testAnalysisScriptName.Trim();

                    TestAnalysisExecutableInfo.Arguments = workingDirectory + @"\testTemp\" + testDataName + " " + workingDirectory + @"\testTemp\" + currentTest.testResultsFileName;
                    TestAnalysisExecutableProcess.Start();
                    TestAnalysisExecutableProcess.WaitForExit(analysisTimeout);
                    if (TestAnalysisExecutableProcess.HasExited == false)
                        TestAnalysisExecutableProcess.Kill();
                }

                if (currentTest.testUseResultsFile == true)
                {
                    try
                    {
                        StreamReader tResult = new StreamReader(workingDirectory + @"\testTemp\" + currentTest.testResultsFileName);
                        string resultLine;

                        resultLine = tResult.ReadLine();
                        while (resultLine != null)
                        {
                            if (resultLine.Contains("result ="))
                            {
                                if (resultLine.Contains("PASS"))
                                    testReceipt.testPass = true;
                                else
                                    testReceipt.testPass = false;
                            }
                            else if (resultLine.Contains("accuracy"))
                            {
                                index = resultLine.IndexOf('=') + 2;
                                testReceipt.testAccuracy = double.Parse(resultLine.Substring(index, resultLine.Length - index));
                            }
                            else if (resultLine.Contains("resultParameter1"))
                            {
                                index = resultLine.IndexOf('=') + 2;
                                testReceipt.testReturnParameter1 = resultLine.Substring(index, resultLine.Length - index);
                            }
                            else if (resultLine.Contains("resultParameter2"))
                            {
                                index = resultLine.IndexOf('=') + 2;
                                testReceipt.testReturnParameter2 = resultLine.Substring(index, resultLine.Length - index);
                            }
                            else if (resultLine.Contains("resultParameter3"))
                            {
                                index = resultLine.IndexOf('=') + 2;
                                testReceipt.testReturnParameter3 = resultLine.Substring(index, resultLine.Length - index);
                            }
                            else if (resultLine.Contains("resultParameter4"))
                            {
                                index = resultLine.IndexOf('=') + 2;
                                testReceipt.testReturnParameter4 = resultLine.Substring(index, resultLine.Length - index);
                            }
                            else if (resultLine.Contains("resultParameter5"))
                            {
                                index = resultLine.IndexOf('=') + 2;
                                testReceipt.testReturnParameter5 = resultLine.Substring(index, resultLine.Length - index);
                                testReceipt.testComplete = true;
                            }
                            resultLine = tResult.ReadLine();
                        }
                        tResult.Close();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("test results read FAIL: " + ex.Message);
                        return "Test result file read failure: " + ex.Message;
                    }
                }


                // delete raw data file
                Thread.Sleep(50);
                Directory.Delete(workingDirectory + @"\" + "testTemp", true);
                #endregion

                currentTest.testState = "Test Complete";
                mainHandle.Dispatcher.BeginInvoke(mainHandle.updateDelegate);

                return "Test completed";
            }
            catch (Exception ex)
            {
                if (telnet != null) telnet.Kill();
                if (gdb != null) gdb.Kill();
                if (git != null) git.Kill();
                for (int indexCOM = 0; indexCOM < maxCOMInstances; indexCOM++)
                {
                    if (COM[indexCOM].active == true)
                        COM[indexCOM].Kill();
                } 
                if (fTest != null) fTest.Kill();
                if (openOCD.active == true) openOCD.Kill();
                if (matlab != null) matlab.Kill();
                if (msbuild != null) msbuild.Kill();
                if (fastboot != null) fastboot.Kill();
                System.Diagnostics.Debug.WriteLine("ExecuteTest FAIL: " + ex.Message);
                return "Exception failure: " + ex.Message;
            }
        }
    }
}
