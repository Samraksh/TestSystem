using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;
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
			GitHub simpHook = (GitHub)GitHubHook;
			foreach (Commit commit in simpHook.commits) {
				if((commit.message).Contains("SamTest")) {
					TestQueue.Enqueue(commit.id);
				}
			}
		}
		public class Commit {
			public string message;
			public string id;
		}
		public class GitHub {
			public Commit[] commits;
		}
	}

	public static class TestQueue {
		// TODO add priority queuing
		private static Queue<string> _ids = new Queue<string>();
		public static Queue<string> IDs{ get{ return _ids; }}

		public static void Enqueue(string id) {
			_ids.Enqueue(id);
		}
		public static string Dequeue(string id) {
			return _ids.Dequeue();
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
			runspace = RunspaceFactory.CreateRunspace();
			runspace.Open();
			Pipeline pipeline = runspace.CreatePipeline();
			pipeline.Commands.AddScript("init.ps1");
			pipeline.Invoke();

		}
		public void Kill() {
			runspace.Close();
		}
		public void RunTest(string id) {
			Pipeline pipeline = runspace.CreatePipeline();
			pipeline.Commands.AddScript("test.ps1");
			runspace.SessionStateProxy.SetVariable("commit_id", id);
			pipeline.Invoke();
		}
	}/** PowerShell **/
}