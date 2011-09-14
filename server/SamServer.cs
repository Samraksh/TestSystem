using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Web;
using System.Web.Script.Serialization;

namespace SamServer {
	public class Service:IHttpHandler {

		/* OUTPUT */
		private StringWriter stdOutput = new StringWriter();
		public StringWriter Output{ get { return stdOutput; }}
		private StringWriter stdError = new StringWriter();
		public StringWriter Error{ get { return stdError; }}

		public bool IsReusable {
			get { throw new NotImplementedException(); }
		}
		public void ProcessRequest(HttpContext context) {
			switch (context.Request.HttpMethod)
			{
				case "GET":
					//Perform READ Operation
					crud_get(context);
					break;
				case "POST":
					//Perform CREATE Operation
					crud_post(context);
					break;
				case "PUT":
					//Perform UPDATE Operation
					break;
				case "DELETE":
					//Perform DELETE Operation
					break;
				default:
					break;
			}
		}
		private static void WriteResponse(string resp) {
			HttpContext.Current.Response.Write(resp);
		}
		private void crud_get(HttpContext context) {
			WriteResponse("face");
		}
		private void crud_post(HttpContext context) {
			WriteResponse("face");
			// The message body is posted as bytes. read the bytes
			byte[] PostData = context.Request.BinaryRead(context.Request.ContentLength);
			//Convert the bytes to string using Encoding class
			string str = Encoding.UTF8.GetString(PostData);
			// deserialize json
			JavaScriptSerializer json = new JavaScriptSerializer();
			Object GitHubHook = json.DeserializeObject(str);
			foreach (object commit in GitHubHook.commits) {
				if(((string)commit.message).Contains("SamTest")) {
					TestQueue.Enqueue((string)commit.id);
				}
			}
		}
	}

	public class TestQueue {
		// TODO add priority queuing
		private static Queue<string> _ids = new Queue<string>();
		public Queue<string> IDs{ get{ return _ids; }}

		public void Enqueue(string id) {
			_ids.Enqueue(id);
		}
		public int Dequeue(string id) {
			return _ids.Dequeue();
		}
	}

	/********************
		Git - Singleton
	*********************/
	public sealed class Git {
		private static readonly Git instance = new PowerShell();
		/* CONSTRUCTOR */
		private Git(){
			Start();
		}
		internal static Git Instance{ get { return instance; }}

		/* PROCESS HANDLERS */
		private StreamWriter input = null;
		private void StandardOutputHandler(object sendingProcess, DataReceivedEventArgs outLine) {
			stdOutput.WriteLine(outLine.Data);
		}
		private void StandardErrorHandler(object sendingProcess, DataReceivedEventArgs errLine){
			stdError.WriteLine(errLine.Data);
		}

		/* OUTPUT */
		private StringWriter stdOutput = new StringWriter();
		public StringWriter Output{ get { return stdOutput; }}
		private StringWriter stdError = new StringWriter();
		public StringWriter Error{ get { return stdError; }}

		/* PROPERTIES */
		private Process process;
		private string checkout_location = @"C:\MicroFrameworkPK_v4_0";

		/* PUBLIC METHODS */
		public void Start() { //(string[] cfgs)
			process = new System.Diagnostics.Process();
			process.StartInfo.FileName = @"git-bash.bat";
			// foreach cfg in cfgs
			process.StartInfo.Arguments = @"";
			process.StartInfo.WorkingDirectory = checkout_location;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			// Streams
			process.StartInfo.RedirectStandardInput = true;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			// Start
			process.Start();
			// StandardInput stream
			input = process.StandardInput;
			// StandardOutput stream
			process.OutputDataReceived += new DataReceivedEventHandler(StandardOutputHandler);
			process.BeginOutputReadLine();
			// StandardError stream
			process.ErrorDataReceived += new DataReceivedEventHandler(StandardErrorHandler);
			process.BeginErrorReadLine();	
		}
		public void Kill() {
			process.Kill();
		}
		public void Checkout(string root, string branch) {
			input.WriteLine(@"cd "+root);
			input.WriteLine(@"git fetch");
			input.WriteLine(@"git checkout "+branch);
		}
	}

	/********************
		PowerShell - Singleton
	*********************/
	public sealed class PowerShell {
		private static readonly PowerShell instance = new PowerShell();
		/* CONSTRUCTOR */
		private PowerShell(){
			Start();
		}
		internal static PowerShell Instance{ get { return instance; }}

		/* OUTPUT */
		private StringWriter stdOutput = new StringWriter();
		public StringWriter Output{ get { return stdOutput; }}
		private StringWriter stdError = new StringWriter();
		public StringWriter Error{ get { return stdError; }}

		/* PROPERTIES */
		public Runspace runspace;

		/* PUBLIC METHODS */
		public void Start() {
			RunspaceConfiguration config = new RunspaceConfiguration();
			config.InitializationScripts = new ScriptConfigurationEntry("SamTest Init", "init.ps1");
			runspace = RunspaceFactory.CreateRunspace(config);
			runspace.Open();
		}
		public void Kill() {

		}
		public void RunTest(string id) {
			Pipeline pipeline = runspace.CreatePipeline();
			pipeline.Commands.AddScript("test.ps1");
			runspace.SessionStateProxy.SetVariable("commit_id", id);
			pipeline.Invoke();
		}
	}/** PowerShell **/
}