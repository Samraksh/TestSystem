using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;

namespace SamTest{

	public class MSBuild {

		public static AutoResetEvent ARE_build = new AutoResetEvent(false);
		private static AutoResetEvent ARE_start = new AutoResetEvent(false);

		private StreamWriter input = null;
		
		private Process p;

		private Regex buildRegEx = new Regex(@"build succeeded.|build failed.", RegexOptions.IgnoreCase);

		private StringWriter stdOutput = new StringWriter();
		public StringWriter Output{get { return stdOutput; }}
		private StringWriter stdError = new StringWriter();
		public StringWriter Error{ get { return stdError;}}

		private void StandardOutputHandler(object sendingProcess, DataReceivedEventArgs outLine) {
			stdOutput.WriteLine(outLine.Data);
			if(buildRegEx.IsMatch(outLine.Data)) {
				stdOutput.WriteLine("face");
				ARE_build.Set();
			}

			Console.WriteLine(outLine.Data);
		}
		private void StandardErrorHandler(object sendingProcess, DataReceivedEventArgs errLine){
			//stdError.WriteLine(errLine.Data);
			Console.WriteLine(errLine.Data);
		}

		public void Start(Object root)
		{

			Console.WriteLine("\nStarting MSBuild !!!");
			ProcessStartInfo startInfo = new ProcessStartInfo();
			p = new Process();
			startInfo.FileName = @"cmd.exe";
			startInfo.WorkingDirectory = (string)root;
			startInfo.UseShellExecute = false;
			startInfo.WindowStyle = ProcessWindowStyle.Normal;
			// Streams
			startInfo.RedirectStandardInput = true;
			startInfo.RedirectStandardOutput = true;
			startInfo.RedirectStandardError = true;
			
			p.StartInfo = startInfo;
			p.Start();
			input = p.StandardInput;
			p.OutputDataReceived += new DataReceivedEventHandler(StandardOutputHandler);
			p.BeginOutputReadLine();
			p.ErrorDataReceived += new DataReceivedEventHandler(StandardErrorHandler);
			p.BeginErrorReadLine();
			ARE_start.Set();
		}

		public  void SetEnv(string gdb_path_root)
		{
			ARE_start.WaitOne();
			input.WriteLine(@"setenv_gcc.cmd "+@gdb_path_root);
		}
		public void Clean(string root, string proj)
		{
			input.WriteLine(@"msbuild "+root+@"\"+proj+@" /target:clean");
			ARE_build.WaitOne();
		}
		public void Build(string root, string proj)
		{
			input.WriteLine(@"msbuild "+root+@"\"+proj+@" /target:build");
			ARE_build.WaitOne();
		}
		public void Kill() {
			p.Kill();
		}

	}

}
