using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.IO;

namespace TestRig
{
    public class PowerShell
    {

        public WSManConnectionInfo connectionInfo;

        public Runspace runspace;

        public Pipeline pipeline;

        public string codeSourceryPath { get; set; }

        public string mfInstallationPath { get; set; }

        //public System.Security.SecureString password;

        public Test tInstance;

        public static string outputFileName = "";

        public PowerShell(string csPath, string tbMFPath)
        {
            this.codeSourceryPath = csPath;
            this.mfInstallationPath = tbMFPath;
           // this.outputFileName = "";
        }

        public bool doesRunSpaceExist()
        {
            if (runspace == null)
                return false;

            return true;
        }

        public string strip(string s)
        {
            if (s == null)
                return "";
            s = s.Replace("\n", String.Empty);
            s = s.Replace("\t", String.Empty);
            s = s.Replace("\r", String.Empty);

            return s;
        }

        public void ConnectLocal()
        {
            connectionInfo = new WSManConnectionInfo();
            connectionInfo.OperationTimeout = 4 * 60 * 1000;
            connectionInfo.OpenTimeout = 1 * 60 * 1000;
        }

        public void ConnectRemote()
        {
            PSCredential remoteMachineCredentials = new PSCredential("Nived",Window1.password);
            string shelluri = "http://schemas.microsoft.com/powershell/Microsoft.PowerShell";
            connectionInfo = new WSManConnectionInfo(false, "nived-lappy", 5985, "/wsman", shelluri, remoteMachineCredentials);

        }

        public void createPowerShellRunspace()
        {
            try
            {
                runspace = RunspaceFactory.CreateRunspace(connectionInfo);
                //SPSecurity.CodeToRunElevated ElevatedCode = new SPSecurity.CodeToRunElevated(OpenRunspace);
                //SPSecurity.RunWithElevatedPrivileges(ElevatedCode);
                runspace.Open();
                

            }
            catch (Exception e)
            {
                Window1.showMessageBox(e.Message);
            }
        }

        public void close()
        {
            pipeline.Stop();
        }

        public void ExecuteEnvScript()
        {
            pipeline = runspace.CreatePipeline();
            //string scriptName = t.testName + "_" + t.testerName + "_" + Convert.ToString(t.getId()) + ".ps1";

            pipeline.Commands.AddScript(tInstance.buildEnvScriptName);
            pipeline.Invoke();
            
        }

        public void ExecuteTestScript()
        {
            pipeline = runspace.CreatePipeline();
            //string scriptName = t.testName + "_" + t.testerName + "_" + Convert.ToString(t.getId()) + ".ps1";

            pipeline.Commands.AddScript(tInstance.buildEnvScriptName);
            pipeline.Invoke();

        }

        public void setTestEnv(Test t)
        {
            this.tInstance = t;

            // Read the location of the test directory
            string testRootDirectory = TestRig.Window1.ToolPath;
            
            // Strip the unnecessary chars being generated during xml reading like \n\r\t
            testRootDirectory = strip(testRootDirectory);

            string testDirectory = testRootDirectory + @"\Logs";

            // Create new directory for testing
            string directoryName = t.testName + t.testType +  t.testerName + Convert.ToString(t.getId()) ;

            outputFileName = testDirectory + "\\" + directoryName + "\\" + directoryName + ".csv";

            DirectoryInfo td = Directory.CreateDirectory(testDirectory + "\\" + directoryName);
            
            this.tInstance.directoryName = testDirectory + "\\" + directoryName;

            // Generate the build env script name
            string buildEnvScriptName = tInstance.directoryName + "\\" +  tInstance.testName + "_" + tInstance.testType + "_" + tInstance.testerName + "_" + Convert.ToString(tInstance.getId()) + "_Env.ps1";
            tInstance.buildEnvScriptName = buildEnvScriptName;
            
            // Fill in the contents of the script
            using (StreamWriter buildEnvScript = new StreamWriter(buildEnvScriptName))
            {
                buildEnvScript.WriteLine("###############################################################################");
                buildEnvScript.WriteLine("# The Samraksh Company");
                buildEnvScript.WriteLine("# This is an auto generated file from tool TestRig ");
                buildEnvScript.WriteLine("###############################################################################");

                buildEnvScript.WriteLine("\n");

                buildEnvScript.WriteLine("mkdir " + tInstance.directoryName);

                buildEnvScript.WriteLine("Copy-Item " + testRootDirectory + @"\EnvGenFiles\msbuild.cs -destination " + tInstance.directoryName);

                buildEnvScript.WriteLine(@"Copy-Item " + testRootDirectory + @"\EnvGenFiles\GDB.cs -destination " + tInstance.directoryName);

                buildEnvScript.WriteLine(@"Copy-Item " + testRootDirectory + @"\EnvGenFiles\GDBCommand.cs -destination " + tInstance.directoryName);

                buildEnvScript.WriteLine(@"cd " + tInstance.directoryName);

                buildEnvScript.WriteLine("$compiler = \"$env:windir/Microsoft.NET/Framework/v2.0.50727/csc\"");

                //buildEnvScript.WriteLine(&$compiler /target:library /out:OpenOCD.dll OpenOCD.cs);

                buildEnvScript.WriteLine("&$compiler /target:library /out:GDB.dll GDB.cs GDBCommand.cs");

                buildEnvScript.WriteLine("&$compiler /target:library /out:MsBuild.dll msbuild.cs");

                buildEnvScript.Close();
            }
        }

        public string getMessageString(string input)
        {
            string messageString = "Echo \"" + input + "\" | Out-File -Append ";

            return messageString;
        }

        public void generateTestScript()
        {
            string outFileName = this.tInstance.testName + "_" + this.tInstance.testerName + "_" + Convert.ToString(this.tInstance.getId()) + "_" + "Out.log";
            string testScriptName = this.tInstance.directoryName + "\\" + this.tInstance.testName + "_" + this.tInstance.testerName + "_" + Convert.ToString(this.tInstance.getId()) + "_Test.ps1";
            tInstance.testScriptName = testScriptName;
            using (StreamWriter script = new StreamWriter(testScriptName))
            {
                script.WriteLine("###############################################################################");
                script.WriteLine("# The Samraksh Company");
                script.WriteLine("# This is an auto generated file from tool TestRig ");
                script.WriteLine("###############################################################################");

                script.WriteLine("Echo \"Executing " + tInstance.testName + "\" | Out-File " + outFileName);

                script.WriteLine("\n");

                script.WriteLine(getMessageString("Stage 1 : Launching Build ") + outFileName);

                script.WriteLine("\n");

                script.WriteLine("$msbuild = [Reflection.Assembly]::LoadFrom(\"msbuild.dll\")");
                script.WriteLine("$gdb = [Reflection.Assembly]::LoadFrom(\"GDB.dll\")");

                script.WriteLine("\n");

                script.WriteLine("$msbuildInst = new-object TestRig.msbuild");
                script.WriteLine("$gdbInst = new-object TestRig.gdb");

                script.WriteLine("\n");

                script.WriteLine("$testPath = \"" + strip(this.tInstance.testPath) + "\"");

                script.WriteLine("\n");

                script.WriteLine("$buildProjName = \"" + strip(this.tInstance.buildProj) + "\"");

                script.WriteLine("\n");

                script.WriteLine("$codeSourceryPath = \"" + this.codeSourceryPath + "\"");

                script.WriteLine("\n");

                script.WriteLine("$mfInstallationPath = \"" + this.mfInstallationPath + "\"");

                script.WriteLine(getMessageString("Stage 1 : Setting Environment") + outFileName);

                script.WriteLine("$msbuildInst.Init($testPath,$buildProjName,$codeSourceryPath,$mfInstallationPath)");

                script.WriteLine("\n");

                script.WriteLine("$msbuildInst.Start()");

                script.WriteLine("\n");

                script.WriteLine("$msbuildInst.SetEnv()");

                script.WriteLine(getMessageString("Stage 1 : Cleaning Project") + outFileName);

                script.WriteLine("\n");

                script.WriteLine("$msbuildInst.Clean()");

                script.WriteLine("\n");

                script.WriteLine(getMessageString("Stage 1 : Building Project") + outFileName);

                script.WriteLine("$msbuildInst.Build()");

                script.WriteLine(getMessageString("Stage 1 : Build Complete") + outFileName);

                script.WriteLine("\n");

                script.WriteLine(getMessageString("Stage 2: Launching Deployment") + outFileName);

                script.WriteLine("\n");

                script.WriteLine("$gdbInst.Init()");

                script.WriteLine("\n");

                script.WriteLine("$gdbInst.Start()");

                script.WriteLine("\n");

                script.WriteLine("$gdbInst.Load()");

            }
        }

    }
}
