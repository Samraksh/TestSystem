using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;

namespace SamTest{

	public class OpenOCD {

		private StringWriter stdOutput = new StringWriter();
		public StringWriter Output{get { return stdOutput; }}
		private StringWriter stdError = new StringWriter();
		public StringWriter Error{ get { return stdError;}}

		private void StandardOutputHandler(object sendingProcess, DataReceivedEventArgs outLine) {
			//stdOutput.WriteLine(outLine.Data);
			Console.WriteLine(outLine.Data);
		}
		private void StandardErrorHandler(object sendingProcess, DataReceivedEventArgs errLine){
			//stdError.WriteLine(errLine.Data);
			Console.WriteLine(errLine.Data);
		}

		public void Start()
		{

			Console.WriteLine("\nStarting OpenOCD !!!");
			ProcessStartInfo startInfo = new ProcessStartInfo();
			Process p = new Process();
			startInfo.CreateNoWindow = true;
			startInfo.RedirectStandardOutput = true;
			startInfo.RedirectStandardInput = true;
			startInfo.UseShellExecute = false;
			startInfo.RedirectStandardOutput = true;
			startInfo.RedirectStandardError = true;
			startInfo.Arguments = @"-f C:\Main\Work\Tools\openocd-0.5.0\interface\olimex-jtag-tiny.cfg -f C:\Main\Work\Tools\openocd-0.5.0\target\stm32xl.cfg";
			startInfo.FileName = @"C:\Main\Work\Tools\openocd-0.5.0\bin\openocd-0.5.0.exe";

			p.StartInfo = startInfo;
			p.Start();
			p.OutputDataReceived += new DataReceivedEventHandler(StandardOutputHandler);
			p.BeginOutputReadLine();
			p.ErrorDataReceived += new DataReceivedEventHandler(StandardErrorHandler);
			p.BeginErrorReadLine();
		}

	}

}
