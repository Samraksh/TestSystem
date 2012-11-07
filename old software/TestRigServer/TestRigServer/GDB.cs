using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.IO;

namespace TestRigServer
{
    public class GDB
    {
        public StreamWriter input = null;

        private static GDB instance;

        public GdbCommandResult lastResult;
        public GdbEvent ev;
        public bool running;

        public string axf { get; set; }

        private StringWriter stdOutput = new StringWriter();
        public StringWriter Output { get { return stdOutput; } }
        private StringWriter stdError = new StringWriter();
        public StringWriter Error { get { return stdError; } }

        private static AutoResetEvent ARE_result = new AutoResetEvent(false);
        private static AutoResetEvent ARE_async = new AutoResetEvent(false);

        public GDB()
        {
        }

        public static GDB Instance
        {
            get
            {
                if (instance == null)
                    instance = new GDB();

                return instance;
            }
        }

        private void StandardOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            //stdOutput.WriteLine(outLine.Data);
            Console.WriteLine(outLine.Data);
            if (!String.IsNullOrEmpty(outLine.Data))
            {
                switch (outLine.Data[0])
                {
                    case '^':
                        lastResult = new GdbCommandResult(outLine.Data);
                        running = (lastResult.Status == CommandStatus.Running);
                        ARE_result.Set();
                        break;
                    case '~':
                    case '&':
                        break;
                    case '*':
                        running = false;
                        ev = new GdbEvent(outLine.Data);
                        ARE_async.Set();
                        break;
                }
            }

        }
        private void StandardErrorHandler(object sendingProcess, DataReceivedEventArgs errLine)
        {
            //stdError.WriteLine(errLine.Data);
            Console.WriteLine(errLine.Data);
        }

        public void Start()
        {

            Console.WriteLine("\nStarting GDB !!!");
            ProcessStartInfo startInfo = new ProcessStartInfo();
            Process p = new Process();
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardInput = true;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.Arguments = @"-quiet -fullname --interpreter=mi2";
            startInfo.FileName = @"C:\Main\Work\Tools\codesourcery\bin\arm-none-eabi-gdb.exe";

            p.StartInfo = startInfo;
            p.Start();
            input = p.StandardInput;
            p.OutputDataReceived += new DataReceivedEventHandler(StandardOutputHandler);
            p.BeginOutputReadLine();
            p.ErrorDataReceived += new DataReceivedEventHandler(StandardErrorHandler);
            p.BeginErrorReadLine();
        }

        public void Init(Test t)
        {
            axf = @"C:\MicroFrameworkPK_v4_0\BuildOutput\THUMB2\GCC4.2\le\FLASH\debug\STM32F10x\bin\RegressionTest.axf";
        }

        public void Load()
        {
            axf = Escape(axf);
            Console.WriteLine(axf);
            RunCommand("-file-exec-and-symbols", Escape(axf));
            RunCommand("-target-select remote localhost:3333");
            RunCommand("monitor soft_reset_halt");
            RunCommand("monitor stm32x unlock 0");
            RunCommand("monitor soft_reset_halt");
            RunCommand("-target-download");
            RunCommand("-exec-continue");
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

        private GdbCommandResult RunCommand(string command, params string[] args)
        {
            input.WriteLine(command + " " + String.Join(" ", args));
            ARE_result.WaitOne();
            return lastResult;
        }
    }

}
