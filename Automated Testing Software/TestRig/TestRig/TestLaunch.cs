using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections.ObjectModel;
using System.IO;
using System.Diagnostics;

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
        public LogicAnalyzer logicTest;
        public Matlab matlab;
        public MSBuild msbuild;
        private TestReceipt testReceipt;        

        public TestLaunch(Queue<TestDescription> testCollection, Mutex collectionMutex, MainWindow passedHandle)
        {
            testCollectionLaunch = testCollection;
            testCollectionMutex = collectionMutex;

            mainHandle = passedHandle;            

            // starting thread to launch any locally queued up tests
            LaunchThread = new Thread(new ThreadStart(LaunchThreadFunction));
            LaunchThread.Start();
            LaunchThread.Name = "Launch Test Thread";            
        }

        public void KillLaunchThread()
        {
            // used when the program is quiting
            System.Diagnostics.Debug.WriteLine("Killing launch test thread.");
            if (LaunchThread != null)
            {
                if (gdb != null) gdb.Kill();
                if (git != null) git.Kill();
                if (openOCD != null) openOCD.Kill();                
                if (telnet != null) telnet.Kill();
                if (matlab != null) matlab.Kill();
                if (msbuild != null) msbuild.Kill();

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
                            if (gdb != null) gdb.Kill();
                            if (openOCD != null) openOCD.Kill();
                            
                            System.Diagnostics.Debug.WriteLine("Test FAILED because of:" + returnReason);
                        }

                        testReceipt.testResult = returnReason;

                        string TRPath = mainHandle.textTestReceiptPath; 
                        testReceipt.WriteFile(TRPath);                                                

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

        private string ExecuteTest(TestDescription currentTest)
        {
            try
            {
                string workingDirectory = mainHandle.textTestSourcePath + "\\" + currentTest.testPath;
                string projectName = currentTest.buildProj;
                int index = projectName.IndexOf('.');
                string strippedName = projectName.Substring(0, index);
                string MFPath;

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
                
                currentTest.testState = "Retrieving code";
                mainHandle.Dispatcher.BeginInvoke(mainHandle.updateDelegate);
                
                git = new Git(mainHandle);
                if (git == null) return "Git failed to load";

                // Checkout code from GitHub if needed
                if (currentTest.testGitOption.Contains("Use local code"))
                {
                    // we do not checkout code if we are using the local copies
                    System.Diagnostics.Debug.WriteLine("Using local code for test.");
                }
                else if (currentTest.testGitOption.Contains("Use archive code"))
                {
                    System.Diagnostics.Debug.WriteLine("Checking out archived code.");
                    if (git.CloneCode() == false) return "Git failed to Clone";
                }
                else if (currentTest.testGitOption.Contains("Use archive branch code"))
                {
                    System.Diagnostics.Debug.WriteLine("Checking out branch <" + currentTest.testGitBranch + "> of archived code.");
                    if (git.CloneCodeBranch(currentTest.testGitBranch) == false) return "Git failed to Clone Branch";
                }
                
                git.Kill();
                
                currentTest.testState = "Building";
                mainHandle.Dispatcher.BeginInvoke(mainHandle.updateDelegate);

                System.Diagnostics.Debug.WriteLine("Building code");
                msbuild = new MSBuild(mainHandle, currentTest.testMFVersionNum);
                if (msbuild == null) return "MSBuild failed to load";

                if (currentTest.testType == "C#")
                {
                    if (currentTest.testUsePrecompiledBinary == String.Empty)
                    {
                        if (msbuild.BuildTinyCLR() == false) return "MSBuild failed to build TinyCLR";
                    }
                    if (msbuild.BuildManagedProject(workingDirectory, currentTest.buildProj) == false) return "MSBuild failed to build managed project";
                }
                else
                {
                    if (msbuild.BuildNativeProject(workingDirectory, currentTest.buildProj) == false) return "MSBuild failed to build native project";
                }

                msbuild.Kill();
                
                // executing the current test
                System.Diagnostics.Debug.WriteLine("Executing test for:  " + currentTest.testName);
                
                openOCD = new OpenOCD(mainHandle);
                if (openOCD == null) return "OpenOCD failed to load";
                gdb = new GDB(mainHandle);
                if (gdb == null) return "GDB failed to load";
                telnet = new TelnetBoard(mainHandle);
                if (telnet == null) return "Telnet failed to load";

                if (telnet.Start() == false) return "Telnet failed to start";
                if (telnet.Clear() == false) return "Telnet failed to clear FLASH";                    
                
                if (currentTest.testType == "C#")
                {
                    currentTest.testState = "Loading MF AXF";
                    mainHandle.Dispatcher.BeginInvoke(mainHandle.updateDelegate);
                    if (currentTest.testUsePrecompiledBinary != String.Empty)
                    {
                        if (gdb.Load(workingDirectory + "\\" + currentTest.testUsePrecompiledBinary) == false) return "GDB failed to load precompiled MF AXF file: " + currentTest.testUsePrecompiledBinary;
                    }
                    else
                    {
                        if (gdb.Load(MFPath + @"\" + @"BuildOutput\THUMB2\GCC4.2\le\FLASH\debug\STM32F10x\bin\tinyclr.axf") == false) return "GDB failed to load MF AXF file";                    
                    }                                        

                    currentTest.testState = "Loading managed code";
                    mainHandle.Dispatcher.BeginInvoke(mainHandle.updateDelegate);

                    if (telnet.Load(MFPath + @"\" + @"BuildOutput\public\Debug\Client\dat" + @"\" + strippedName + "_Conv.s19") == false) return "Telnet failed to load";                    
                } else
                {
                    currentTest.testState = "Loading test AXF";
                    mainHandle.Dispatcher.BeginInvoke(mainHandle.updateDelegate);
                    if (currentTest.testUsePrecompiledBinary != String.Empty)
                    {
                        if (gdb.Load(workingDirectory + "\\" + currentTest.testUsePrecompiledBinary) == false) return "GDB failed to load precompiled AXF file: " + currentTest.testUsePrecompiledBinary;
                    }
                    else
                    {
                        if (gdb.Load(MFPath + @"\" + @"BuildOutput\THUMB2\GCC4.2\le\FLASH\debug\STM32F10x\bin" + @"\" + strippedName + ".axf") == false) return "GDB failed to load compiled AXF";
                    }                     
                }
                               
                currentTest.testState = "Starting processor";
                mainHandle.Dispatcher.BeginInvoke(mainHandle.updateDelegate);
                if (gdb.Continue() == false) return "GDB failed to start processor";
                // waiting for processor code to start executing (this can take up to two seconds)
                Thread.Sleep(1000);
                
                //Thread.Sleep(5000);
                currentTest.testState = "Running test";
                mainHandle.Dispatcher.BeginInvoke(mainHandle.updateDelegate);
                
                // Grabbing data for sample time length                
                ReadParameters readVars;
                
                if (currentTest.testType == "C#")
                    readVars = new ReadParameters(workingDirectory + "\\" + "Parameters.cs", currentTest);
                else
                    readVars = new ReadParameters(workingDirectory + "\\" + "Parameters.h", currentTest);

                System.Diagnostics.Debug.WriteLine("useLogic: " + currentTest.testUseLogic.ToString());
                System.Diagnostics.Debug.WriteLine("sampleTimeMs: " + currentTest.testSampleTimeMs.ToString());
                System.Diagnostics.Debug.WriteLine("sampleFrequency: " + currentTest.testSampleFrequency.ToString());
                System.Diagnostics.Debug.WriteLine("useExecutable: " + currentTest.testUseExecutable.ToString());
                System.Diagnostics.Debug.WriteLine("executableName: " + currentTest.testExecutableName.ToString());
                System.Diagnostics.Debug.WriteLine("executableTimeoutMs: " + currentTest.testExecutableTimeoutMs.ToString());

                if (Directory.Exists(workingDirectory + "\\" + "testTemp")) Directory.Delete(workingDirectory + "\\" + "testTemp", true);
                Directory.CreateDirectory(workingDirectory + "\\" + "testTemp");
                if (currentTest.testUseLogic == true)
                {
                    if (logicTest == null)
                        logicTest = new LogicAnalyzer(currentTest.testSampleFrequency, workingDirectory + "\\" + strippedName + ".hkp");
                    else
                        logicTest.Initialize(currentTest.testSampleFrequency, workingDirectory + "\\" + strippedName + ".hkp");                    
                    if (logicTest == null) return "Logic Analyzer failed to load";
                    if (logicTest.startMeasure(workingDirectory + "\\testTemp\\" + "testData.csv", currentTest.testSampleTimeMs) == false) return "Logic Analyzer failed to start measuring";
                    Thread.Sleep(currentTest.testSampleTimeMs);
                    if (logicTest.stopMeasure() == false) return "Logic Analyzer failed to stop measuring";
                }

                if (currentTest.testUseExecutable == true)
                {
                    if (File.Exists(workingDirectory + "\\" + currentTest.testExecutableName) == false) return "Specified executable: " + currentTest.testExecutableName + " does not exist.";
                    ProcessStartInfo TestExecutableInfo = new ProcessStartInfo();
                    Process TestExecutableProcess = new Process();

                    System.Diagnostics.Debug.WriteLine("Starting to run test executable: " + currentTest.testExecutableName);

                    TestExecutableInfo.CreateNoWindow = true;
                    TestExecutableInfo.RedirectStandardInput = true;
                    TestExecutableInfo.UseShellExecute = false;
                    TestExecutableInfo.FileName = workingDirectory + "\\" + currentTest.testExecutableName;

                    TestExecutableProcess.StartInfo = TestExecutableInfo;
                    TestExecutableProcess.Start();
                    TestExecutableProcess.WaitForExit(currentTest.testExecutableTimeoutMs);
                }

                if (telnet != null) telnet.Kill();
                if (gdb != null) gdb.Kill();
                if (openOCD != null) openOCD.Kill();             
                
                currentTest.testState = "Analyzing test";
                mainHandle.Dispatcher.BeginInvoke(mainHandle.updateDelegate);
                
                Thread.Sleep(50);

                if (currentTest.testMatlabAnalysis == true)
                {
                    matlab = new Matlab(mainHandle, testReceipt);
                    if (matlab == null) return "Matlab failed to load";
                    if (matlab.matlabRunScript(workingDirectory, "testTemp\\testData.csv", currentTest) == false) return "Matlab failed to run script";
                }
                else if (currentTest.testPowershellAnalysis == true)
                {
                }

                // delete raw data file
                Thread.Sleep(50);
                Directory.Delete(workingDirectory + "\\" + "testTemp", true);                                    
                
                currentTest.testState = "Test Complete";
                mainHandle.Dispatcher.BeginInvoke(mainHandle.updateDelegate);

                return "Test completed";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ExecuteTest FAIL: " + ex.Message);
                return "Exception failure: " + ex.Message;
            }
        }
    }
}
