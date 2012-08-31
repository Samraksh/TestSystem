using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace TestRigServer
{
    public class OpenOCD
    {
        // Instance of openocd, being singleton will only contain one instance.
        private static OpenOCD instance;

        private string exe;

        private string intrfce;

        private string target;

        private string outMessages;

        private string errMessages;

        private ProcessStartInfo openOCDInfo;

        private OpenOCD() { }

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

        public static OpenOCD Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new OpenOCD();
                }
                return instance;
            }
        }

        public string Exe
        {
            get
            {
                return this.exe;
            }
            set
            {
                this.exe = value;
            }
        }

        public string Intrfce
        {
            get
            {
                return this.intrfce;
            }
            set
            {
                this.intrfce = value;
            }
        }

        public string Target
        {
            get
            {
                return this.target;
            }
            set
            {
                this.target = value;
            }
        }

        private void StandardOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            this.outMessages += "";
            this.outMessages += outLine.Data;
            //stdOutput.WriteLine(outLine.Data);
            //Console.WriteLine(outLine.Data);
        }
        private void StandardErrorHandler(object sendingProcess, DataReceivedEventArgs errLine)
        {
            this.errMessages += "";
            this.errMessages += errLine.Data + "\n";
            //stdError.WriteLine(errLine.Data);
            //Console.WriteLine(errLine.Data);
        }

        public void start()
        {
            this.openOCDInfo = new ProcessStartInfo();
            Process p = new Process();

            openOCDInfo.CreateNoWindow = true;
            openOCDInfo.RedirectStandardOutput = true;
            openOCDInfo.RedirectStandardInput = true;
            openOCDInfo.UseShellExecute = false;
            openOCDInfo.RedirectStandardError = true;
            openOCDInfo.Arguments = @"-f " + this.intrfce + " -f " + this.target;
            openOCDInfo.FileName = this.exe;

            p.OutputDataReceived += new DataReceivedEventHandler(StandardOutputHandler);


            p.ErrorDataReceived += new DataReceivedEventHandler(StandardErrorHandler);


            p.StartInfo = openOCDInfo;
            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();


        }
    }
}
