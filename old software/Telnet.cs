/* Name : Telnet.cs
 *
 * Author : Nived.Sivadas@samraksh.com
 *
 * Description : This file is an alternate way to load code into remote devices using telnet as opposed to Nick.Knudson  * implementation using GDB to load code. 
 *
 *
 */
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Threading;

// The class will be a part of the Samtest namespace which houses all the different components of the testing framework // which were implemented by Nick.Knudson

namespace SamTest{

	// Class Telnet
	public class Telnet {

		// Creates a new instance of StreamWrite named input using default encoding and buffer size
		public StreamWriter input = null;


		private StringWriter stdOutput = new StringWriter();
		public StringWriter Output{get { return stdOutput; }}
		private StringWriter stdError = new StringWriter();
		public StringWriter Error{ get { return stdError;}}

		private static AutoResetEvent ARE_result = new AutoResetEvent(false);
		private static AutoResetEvent ARE_async = new AutoResetEvent(false);
		private static AutoResetEvent sync = mew AutoResetEvent(false);

		private void StandardOutputHandler(object sendingProcess, DataReceivedEventArgs outLine) {
			//stdOutput.WriteLine(outLine.Data);
			Console.WriteLine(outLine.Data);
			

		}
		private void StandardErrorHandler(object sendingProcess, DataReceivedEventArgs errLine){
			//stdError.WriteLine(errLine.Data);
			Console.WriteLine(errLine.Data);
		}
		
		// This function starts an independent process and starts up telnet
		public void Start()
		{
			
			Console.WriteLine("\n Loading with Telnet");
			
			ProcessStartInfo startInfo = new ProcessStartInfo();
			Process p = new Process();
			startInfo.RedirectStandardOutput = true;
			startInfo.WindowStyle = ProcessWindowStyle.Normal;
			startInfo.RedirectStandardInput = true;
			startInfo.UseShellExecute = false;
			startInfo.RedirectStandardError = true;
			startInfo.FileName = @"cmd.exe";

			p.StartInfo = startInfo;
			p.Start();
			input = p.StandardInput;
			p.OutputDataReceived += new DataReceivedEventHandler(StandardOutputHandler);
			p.BeginOutputReadLine();
			p.ErrorDataReceived += new DataReceivedEventHandler(StandardErrorHandler);
			p.BeginErrorReadLine();
			input.WriteLine("telnet localhost 4444");
		}
		public void Load(string axf)
		{
			/*
			axf = Escape(axf);
			Console.WriteLine(axf);
			RunCommand("-file-exec-and-symbols", axf);
			RunCommand("-target-select remote localhost:3333");
			RunCommand("monitor reset init");
			RunCommand("monitor stm32x unlock 0");
			RunCommand("monitor reset init");
			RunCommand("-target-download");
			RunCommand("-break-insert", Escape("ApplicationEntryPoint"));
			RunCommand("-exec-continue");
			ARE_async.WaitOne();
			ARE_async.WaitOne();
			RunCommand("-break-insert", Escape("ApplicationEntryPoint"));
			RunCommand("-exec-jump", Escape("ApplicationEntryPoint"));
			ARE_async.WaitOne();
			ARE_async.WaitOne();
			RunCommand("-exec-finish");
			ARE_async.WaitOne();
			ARE_async.WaitOne();
			*/
		}

		private static string Escape(string str)
		{
			if (str == null)
				return null;
			else if (str.IndexOf (' ') != -1 || str.IndexOf ('"') != -1) {
				str = str.Replace ("\"", "\\\"");
				return "\"" + str + "\"";
			}
			else
				return str;
		}


	}

}
