using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO;

namespace TestRigServer
{
    public class msbuild
    {
        public static AutoResetEvent ARE_build = new AutoResetEvent(false);
        private static AutoResetEvent ARE_start = new AutoResetEvent(false);

        private StreamWriter input = null;

        private Process p;

        public string mfInstalltionPath { get; set; }

        private string rootPath;

        private string testProjName;

        private string outMessages;

        private string errMessages;

        private static msbuild instance;

        public string codeSourceryPath { get; set; }

        private Regex buildRegEx = new Regex(@"build succeeded.|build failed.", RegexOptions.IgnoreCase);

        private StringWriter stdOutput = new StringWriter();
        public StringWriter Output { get { return stdOutput; } }
        private StringWriter stdError = new StringWriter();
        public StringWriter Error { get { return stdError; } }

        public string OutMessages
        {
            get
            {
                return this.outMessages;
            }
            set
            {
                this.outMessages = value;
            }
        }

        public string ErrMessages
        {
            get
            {
                return this.errMessages;
            }
            set
            {
                this.errMessages = value;
            }
        }

        private msbuild()
        {

            outMessages = "";
            errMessages = "";
        }

        public static msbuild Instance
        {
            get
            {
                if (instance == null)
                    instance = new msbuild();

                return instance;
            }
        }

        public void Init(Test t)
        {
            rootPath = t.testPath;
            testProjName = t.buildProj;
        }

        private void StandardOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {


            stdOutput.WriteLine(outLine.Data);
            if (buildRegEx.IsMatch(outLine.Data))
            {
                stdOutput.WriteLine("face");
                ARE_build.Set();
            }
            this.outMessages += "";
            this.outMessages += outLine.Data;

            //Console.WriteLine(outLine.Data);
        }
        private void StandardErrorHandler(object sendingProcess, DataReceivedEventArgs errLine)
        {
            this.ErrMessages += "";
            this.ErrMessages += errLine.Data;
            //stdError.WriteLine(errLine.Data);
            //Console.WriteLine(errLine.Data);
        }

        public void Start()
        {

            Console.WriteLine("\nStarting MSBuild !!!");
            ProcessStartInfo startInfo = new ProcessStartInfo();
            p = new Process();
            startInfo.FileName = @"cmd.exe";
            startInfo.WorkingDirectory = (string)rootPath;
            startInfo.UseShellExecute = false;
            startInfo.WindowStyle = ProcessWindowStyle.Normal;
            // Streams
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            p.StartInfo = startInfo;
            //input = p.StandardInput;
            p.OutputDataReceived += new DataReceivedEventHandler(StandardOutputHandler);
            p.ErrorDataReceived += new DataReceivedEventHandler(StandardErrorHandler);
            p.Start();
            input = p.StandardInput;


            p.BeginOutputReadLine();

            p.BeginErrorReadLine();
            ARE_start.Set();
        }

        public void SetEnv()
        {
            ARE_start.WaitOne();
            input.WriteLine(@"cd " + @mfInstalltionPath);
            input.WriteLine(@"setenv_gcc.cmd " + @codeSourceryPath);
        }
        public void Clean()
        {
            input.WriteLine(@"msbuild " + rootPath + @"\" + testProjName + @" /target:clean");
            ARE_build.WaitOne();
        }
        public void Build()
        {
            input.WriteLine(@"msbuild " + rootPath + @"\" + testProjName + @" /target:build");
            ARE_build.WaitOne();
        }
        public void Kill()
        {
            p.Kill();
        }
    }
}
