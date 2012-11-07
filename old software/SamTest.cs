using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
// external
using SaleaeDeviceSdkDotNet;
using Textile;

/********************
	SamTest Namespace
*********************/
namespace SamTest {

	/********************
		Git - Singleton
	*********************/
	public sealed class Git {
		private static readonly Git instance = new Git();
		/* CONSTRUCTOR */
		private Git(){
		}
		internal static Git Instance{ get { return instance; }}

		/* PROCESS HANDLERS */
		public StreamWriter input = null;
		private void StandardOutputHandler(object sendingProcess, DataReceivedEventArgs outLine) {
			stdOutput.WriteLine(outLine.Data);
			if(outLine.Data.StartsWith("Total")) {
				ARE_done.Set();
			}
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
		private static AutoResetEvent ARE_done = new AutoResetEvent(false);

		/* PUBLIC METHODS */
		public void Start() { //(string[] cfgs)
			process = new System.Diagnostics.Process();
			process.StartInfo.FileName = @"cmd.exe";
			// foreach cfg in cfgs
			process.StartInfo.WorkingDirectory = checkout_location;
			//process.StartInfo.EnvironmentVariables.Add("PATH", @"C:\SamTest\git\cmd")
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			// Streams
			process.StartInfo.RedirectStandardInput = true;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			// Start
            // Try catch block is used to avoid crashes in case of in ability to start openocd
            try
            {
                process.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine("\n\nUnable to start Git !!! ");
            }
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
			input.WriteLine(@"git fetch github");
			ARE_done.WaitOne();
			input.WriteLine(@"git checkout "+branch);
		}
	}

	/********************
		OpenOCD - Singleton
	*********************/
	public sealed class OpenOCD {
		private static readonly OpenOCD instance = new OpenOCD();
		/* CONSTRUCTOR */
		private OpenOCD(){
		}
		internal static OpenOCD Instance{ get { return instance; }}
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

		/********************
			Controller
		*********************/
		public class Controller {
			/* CONTROLLER PROPERTIES */
			private bool m_haveControl = false;
			public bool haveControl{ get { return m_haveControl; }}
			private OpenOCD openocd = null;
			public OpenOCD handle{ get { return openocd; }}

			/* CONTROLLER METHODS */
			public bool Request() {
				m_haveControl = OpenOCD.Request(this, out openocd);
				return m_haveControl;
			}
			public bool Relinquish() {
				m_haveControl = OpenOCD.Relinquish(this, out openocd);
				return m_haveControl;
			}
		}/** Controller **/
		/* CONTROLLER INTERNAL */
		private static Controller nullController = new Controller();
		private static Controller currentController = nullController;
		internal static bool Request(Controller r, out OpenOCD rInstance) {
			if(object.ReferenceEquals(currentController, nullController)) {
				currentController = r;
				rInstance = instance;
				return true;
			} else {
				rInstance = null;
				return false;
			}
		}
		internal static bool Relinquish(Controller r, out OpenOCD rInstance) {
			if(object.ReferenceEquals(currentController, r)) {
				currentController = nullController;
			}
			rInstance = null;
			return false;
		}

		/* PROPERTIES */
		private Process process;
		private string openocd_interface_cfg = @"interface\olimex-jtag-tiny.cfg";
		private string openocd_board_cfg = @"target\stm32.cfg";

		/* PUBLIC METHODS */
		public void Start() { //TODO (string[] cfgs)
			process = new System.Diagnostics.Process();
            process.StartInfo.CreateNoWindow = false;
			process.StartInfo.FileName = @"openocd-0.5.0.exe";
			// foreach cfg in cfgs
			process.StartInfo.Arguments = @"-f "+openocd_interface_cfg+@" -f "+openocd_board_cfg;
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
	}/** OpenOCD **/

	/********************
		GDB - Singleton
	*********************/
	public sealed class GDB {
		private static readonly GDB instance = new GDB();
		/* CONSTRUCTOR */
		private GDB(){
		}
		internal static GDB Instance{ get { return instance; }}
		/* PROCESS HANDLERS */
		private StreamWriter input = null;
		private void StandardOutputHandler(object sendingProcess, DataReceivedEventArgs outLine) {
			stdOutput.WriteLine(outLine.Data);
			if(!String.IsNullOrEmpty(outLine.Data)) {
				switch (outLine.Data [0]) {
					case '^':
						lastResult = new GdbCommandResult (outLine.Data);
						running = (lastResult.Status == CommandStatus.Running);
						ARE_result.Set();
						break;
					case '~':
					case '&':
						break;
					case '*':
						running = false;
						ev = new GdbEvent (outLine.Data);
						ARE_async.Set();
						break;
				}
			}
		}
		private void StandardErrorHandler(object sendingProcess, DataReceivedEventArgs errLine){
			stdError.WriteLine(errLine.Data);
		}

		/* OUTPUT */
		private StringWriter stdOutput = new StringWriter();
		public StringWriter Output{ get { return stdOutput; }}
		private StringWriter stdError = new StringWriter();
		public StringWriter Error{ get { return stdError; }}

		/********************
			Controller
		*********************/
		public class Controller {
			/* CONTROLLER PROPERTIES */
			private bool m_haveControl = false;
			public bool haveControl{ get { return m_haveControl; }}
			private GDB gdb = null;
			public GDB handle{ get { return gdb; }}

			/* CONTROLLER METHODS */
			public bool Request() {
				m_haveControl = GDB.Request(this, out gdb);
				return m_haveControl;
			}
			public bool Relinquish() {
				m_haveControl = GDB.Relinquish(this, out gdb);
				return m_haveControl;
			}
		}/** Controller **/
		/* CONTROLLER INTERNAL */
		private static Controller nullController = new Controller();
		private static Controller currentController = nullController;
		internal static bool Request(Controller r, out GDB rInstance) {
			if(object.ReferenceEquals(currentController, nullController)) {
				currentController = r;
				rInstance = instance;
				return true;
			} else {
				rInstance = null;
				return false;
			}
		}
		internal static bool Relinquish(Controller r, out GDB rInstance) {
			if(object.ReferenceEquals(currentController, r)) {
				currentController = nullController;
			}
			rInstance = null;
			return false;
		}

		/* PROPERTIES */
		private static AutoResetEvent ARE_result = new AutoResetEvent(false);
		private static AutoResetEvent ARE_async = new AutoResetEvent(false);
		// TODO make private with gets{}
		public GdbCommandResult lastResult;
		public GdbEvent ev;
		public bool running;

		/* EVENT: ResultRecord */
		public class ResultRecordEventArgs: EventArgs {
			public ResultRecordEventArgs() {
			}
		}
		public event EventHandler<ResultRecordEventArgs> ResultRecordRecieved;
		private void OnResultRecordRecieved(ResultRecordEventArgs e){
			if(ResultRecordRecieved != null) {
				ResultRecordRecieved(this, e);
			}
		}
		/* EVENT: Connected */
		private Process process;
		public event EventHandler<EventArgs> AsyncRecordRecieved;
		private void OnAsyncRecordRecieved(EventArgs e){
			if(AsyncRecordRecieved != null) {
				AsyncRecordRecieved(this, e);
			}
		}
		/* PUBLIC METHODS */
		public void Start(object root) {
			process = new System.Diagnostics.Process();
			process.StartInfo.FileName = @"arm-none-eabi-gdb.exe";
			process.StartInfo.Arguments = @"-quiet -fullname --interpreter=mi2";
			process.StartInfo.WorkingDirectory = (string)root;
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
		public void Connect(string axf) {
			RunCommand("-file-exec-and-symbols", Escape(axf));
			RunCommand("-target-select remote localhost:3333");
			RunCommand("monitor reset init");
		}
		public void Load() {
			RunCommand("monitor stm32x unlock 0");
			RunCommand("monitor reset init");
			RunCommand("-target-download");
		}
		public void ContinueTo(string entry) {
			RunCommand("-break-insert", Escape(entry));
			RunCommand("-exec-continue");
			ARE_async.WaitOne();
			ARE_async.WaitOne();
		}
		public void JumpTo(string entry) {
			RunCommand("-break-insert", Escape(entry));
			RunCommand("-exec-jump", Escape(entry));
			ARE_async.WaitOne();
			ARE_async.WaitOne();
		}
		public void Execute() {
			RunCommand("-exec-finish");
			ARE_async.WaitOne();
			ARE_async.WaitOne();
		}

		/* PRIVATE METHODS */
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
		private GdbCommandResult RunCommand(string command, params string[] args) {
			input.WriteLine(command + " " + String.Join (" ", args));
			ARE_result.WaitOne();
			return lastResult;
		}
	}/** GDB **/

	/********************
		MSBuild - Singleton
	*********************/
	public sealed class MSBuild {
		private static readonly MSBuild instance = new MSBuild();
		/* CONSTRUCTOR */
		private MSBuild(){
		}
		internal static MSBuild Instance{ get { return instance; }}
		/* PROCESS HANDLERS */
		private StreamWriter input = null;
		private Regex buildRegEx = new Regex(@"build succeeded.|build failed.", RegexOptions.IgnoreCase);
		private void StandardOutputHandler(object sendingProcess, DataReceivedEventArgs outLine) {
			stdOutput.WriteLine(outLine.Data);
			if(buildRegEx.IsMatch(outLine.Data)) {
				stdOutput.WriteLine("face");
				ARE_build.Set();
			}
		}
		private void StandardErrorHandler(object sendingProcess, DataReceivedEventArgs errLine){
			stdError.WriteLine(errLine.Data);
		}

		/* OUTPUT */
		private StringWriter stdOutput = new StringWriter();
		public StringWriter Output{ get { return stdOutput; }}
		private StringWriter stdError = new StringWriter();
		public StringWriter Error{ get { return stdError; }}

		/********************
			Controller
		*********************/
		public class Controller {
			/* CONTROLLER PROPERTIES */
			private bool m_haveControl = false;
			public bool haveControl{ get { return m_haveControl; }}
			private MSBuild msbuild = null;
			public MSBuild handle{ get { return msbuild; }}

			/* CONTROLLER METHODS */
			public bool Request() {
				m_haveControl = MSBuild.Request(this, out msbuild);
				return m_haveControl;
			}
			public bool Relinquish() {
				m_haveControl = MSBuild.Relinquish(this, out msbuild);
				return m_haveControl;
			}
		}/** Controller **/
		/* CONTROLLER INTERNAL */
		private static Controller nullController = new Controller();
		private static Controller currentController = nullController;
		internal static bool Request(Controller r, out MSBuild rInstance) {
			if(object.ReferenceEquals(currentController, nullController)) {
				currentController = r;
				rInstance = instance;
				return true;
			} else {
				rInstance = null;
				return false;
			}
		}
		internal static bool Relinquish(Controller r, out MSBuild rInstance) {
			if(object.ReferenceEquals(currentController, r)) {
				currentController = nullController;
			}
			rInstance = null;
			return false;
		}

		/* PROPERTIES */
		private Process process;
		private static AutoResetEvent ARE_build = new AutoResetEvent(false);
		private static AutoResetEvent ARE_start = new AutoResetEvent(false);

		/* PUBLIC METHODS */
		public void Start(object root) {
			process = new System.Diagnostics.Process();
			process.StartInfo.FileName = @"cmd.exe";
			process.StartInfo.WorkingDirectory = (string)root;
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
			ARE_start.Set();
		}
		public void SetEnv(string gdb_path_root) {
			ARE_start.WaitOne();
			input.WriteLine(@"setenv_gcc.cmd "+@gdb_path_root);
		}
		public void Clean(string root, string proj) {
			input.WriteLine(@"msbuild "+root+@"\"+proj+@" /target:clean");
			ARE_build.WaitOne();
		}
		public void Build(string root, string proj) {
			input.WriteLine(@"msbuild "+root+@"\"+proj+@" /target:build");
			ARE_build.WaitOne();
		}
		public void Kill() {
			process.Kill();
		}

		/* PRIVATE METHODS */
	}/** MSBuild **/

	/********************
		Logic - Singleton
	*********************/
	public sealed class Logic {
		private static readonly Logic instance = new Logic();
		/* CONSTRUCTOR */
		private Logic(){
		}
		internal static Logic Instance{ get { return instance; }}

		/* OUTPUT */
		private StringWriter stdOutput = new StringWriter();
		public StringWriter Output{ get { return stdOutput; }}
		private StringWriter stdError = new StringWriter();
		public StringWriter Error{ get { return stdError; }}

		/********************
			Controller
		*********************/
		public class Controller {
			/* CONTROLLER PROPERTIES */
			private bool m_haveControl = false;
			public bool haveControl{ get { return m_haveControl; }}
			private Logic logic = null;
			public Logic handle{ get { return logic; }}

			/* CONTROLLER METHODS */
			public bool Request() {
				m_haveControl = Logic.Request(this, out logic);
				return m_haveControl;
			}
			public bool Relinquish() {
				m_haveControl = Logic.Relinquish(this, out logic);
				return m_haveControl;
			}
		}/** Controller **/
		/* CONTROLLER INTERNAL */
		private static Controller nullController = new Controller();
		private static Controller currentController = nullController;
		internal static bool Request(Controller r, out Logic rInstance) {
			if(object.ReferenceEquals(currentController, nullController)) {
				currentController = r;
				rInstance = instance;
				return true;
			} else {
				rInstance = null;
				return false;
			}
		}
		internal static bool Relinquish(Controller r, out Logic rInstance) {
			if(object.ReferenceEquals(currentController, r)) {
				currentController = nullController;
			}
			rInstance = null;
			return false;
		}

		/* PROPERTIES */
		public static UInt32 RateInv = 1/1000000;
		private UInt32 mSampleRateHz = 1000000;
		private MLogic mLogic;

		/* EVENT: Connected */
		public event EventHandler<EventArgs> Connected;
		private void OnConnected(EventArgs e){
			if(Connected != null)
				Connected(this, e);
		}

		/* PUBLIC METHODS */
		public void Start() {
			MSaleaeDevices devices = new MSaleaeDevices();
			devices.OnLogicConnect += new MSaleaeDevices.OnLogicConnectDelegate(LogicOnConnect);
			devices.OnDisconnect += new MSaleaeDevices.OnDisconnectDelegate(LogicOnDisconnect);
			devices.BeginConnect();
			stdOutput.WriteLine("Waiting for Logic connection.");
		}
		public void AttachOnReadData(MLogic.OnReadDataDelegate fn) {
			mLogic.OnReadData += new MLogic.OnReadDataDelegate(fn);
		}
		public void DetachOnReadData(MLogic.OnReadDataDelegate fn) {
			mLogic.OnReadData -= new MLogic.OnReadDataDelegate(fn);
		}
		// TODO detach
		public bool SetSampleRate(UInt32 mSampleRateHz) {
			if(!isRunning()) {
				mLogic.SampleRateHz = mSampleRateHz;
				return true;
			} else {
				// can't - logic is running
				return false;
			}
		}
		public bool ReadStart() {
			if(!isRunning()) {
				mLogic.ReadStart();
				return true;
			} else {
				// can't - logic is running
				return false;
			}
		}
		public bool ReadStop() {
			if(isRunning()) {
				mLogic.Stop();
				return true;
			} else {
				// can't - logic isn't running
				return false;
			}
		}
		public bool isRunning() {
			return mLogic.IsStreaming();
		}
		public bool ReadByte(out byte outByte) {
			if(!isRunning()) {
				outByte = mLogic.GetInput();
				return true;
			} else {
				// can't - logic is running
				outByte = 0;
				return false;
			}
		}

		/* PRIVATE METHODS */
		private void LogicOnDisconnect(ulong device_id){
			stdOutput.WriteLine("Logic with id {0} disconnected.", device_id);
			if (mLogic != null)
					mLogic = null;
		}
		private void LogicOnConnect(ulong device_id, MLogic logic){
			stdOutput.WriteLine("Logic with id {0} connected.", device_id);
			mLogic = logic;
			//mLogic.OnReadData += new MLogic.OnReadDataDelegate(mLogic_OnReadData);
			//mLogic.OnWriteData += new MLogic.OnWriteDataDelegate(mLogic_OnWriteData);
			mLogic.OnError += new MLogic.OnErrorDelegate(mLogic_OnError);
			mLogic.SampleRateHz = mSampleRateHz;
			// invoke the Connected event
			OnConnected(EventArgs.Empty);
		}
		private void mLogic_OnReadData(ulong device_id, byte[] data){
		}
		private void mLogic_OnWriteData(ulong device_id, byte[] data) {
		}
		private void mLogic_OnError(ulong device_id){
			stdError.WriteLine("Error thrown.");
		}
	}/** Logic **/

	/********************
		Parser - Singleton
	*********************/
	public sealed class Parser {
		private static readonly Parser instance = new Parser();
		/* CONSTRUCTOR */
		private Parser() {
		}
		internal static Parser Instance{ get { return instance; }}
		/* PROCESS HANDLERS */
		private StreamWriter input = null;
		private void StandardOutputHandler(object sendingProcess, DataReceivedEventArgs outLine) {
			stdOutput.WriteLine(outLine.Data);
			if(!String.IsNullOrEmpty(outLine.Data)) {
				if(outLine.Data == "Parse complete."){
					// Invoke the Parsed event
					ARE_parsed.Set();
					return;
				}
				// Invoke the EventParsed event
				OnEventParsed(outLine.Data);
			}
		}
		private void StandardErrorHandler(object sendingProcess, DataReceivedEventArgs errLine){
			stdError.WriteLine(errLine.Data);
		}

		/* OUTPUT */
		private StringWriter stdOutput = new StringWriter();
		public StringWriter Output{ get { return stdOutput; }}
		private StringWriter stdError = new StringWriter();
		public StringWriter Error{ get { return stdError; }}

		/********************
			Controller
		*********************/
		public class Controller {
			/* CONTROLLER PROPERTIES */
			private bool m_haveControl = false;
			public bool haveControl{ get { return m_haveControl; }}
			private Parser parser = null;
			public Parser handle{ get { return parser; }}

			/* CONTROLLER METHODS */
			public bool Request() {
				m_haveControl = Parser.Request(this, out parser);
				return m_haveControl;
			}
			public bool Relinquish() {
				m_haveControl = Parser.Relinquish(this, out parser);
				return m_haveControl;
			}
		}/** Controller **/
		/* CONTROLLER INTERNAL */
		private static Controller nullController = new Controller();
		private static Controller currentController = nullController;
		internal static bool Request(Controller r, out Parser rInstance) {
			if(object.ReferenceEquals(currentController, nullController)) {
				currentController = r;
				rInstance = instance;
				return true;
			} else {
				rInstance = null;
				return false;
			}
		}
		internal static bool Relinquish(Controller r, out Parser rInstance) {
			if(object.ReferenceEquals(currentController, r)) {
				currentController = nullController;
			}
			rInstance = null;
			return false;
		}

		/* PROPERTIES */
		private Process process;
		private string script = @"parser.py";
		private static AutoResetEvent ARE_parsed = new AutoResetEvent(false);

		/* EVENT: EventParsed */
		public delegate void EventParsedDelegate(string eventLine);
		public event EventParsedDelegate EventParsed;
		private void OnEventParsed(string eventLine){
			if(EventParsed != null)
				EventParsed(eventLine);
		}

		/* PUBLIC METHODS */
		public void Parse(object path_filename) {
			process = new Process();
			process.StartInfo.FileName = @"C:\Python32\python.exe";
			process.StartInfo.Arguments = script + @" " + path_filename;
			process.StartInfo.WorkingDirectory = @"C:\SamTest\parser";
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
			ARE_parsed.WaitOne();
		}
		public void AttachEventParsed(EventParsedDelegate fn) {
			EventParsed += new EventParsedDelegate(fn);
		}
		public void DetachEventParsed(EventParsedDelegate fn) {
			EventParsed -= new EventParsedDelegate(fn);
		}
		// TODO detach event

		/* PRIVATE METHODS */
	}/** Parser **/

	/********************
		SuiteInstance
	*********************/
	public class SuiteInstance {

		public class Feedback {
			public Feedback(string root) {
				this.root = root;
			}

			private string root;

			private StringWriter str = new StringWriter();
			public StringWriter preTextile{ get { return str; }}
			private string _textile;
			public string textile{get { return _textile; }}

			public void AddAsFile(string name, string content) {
				using (FileStream fs = File.Create(root + @"\" + name)) {
					AddText(fs, content);
				}
				str.WriteLine("\"" + name + "\":" + name);
				// append to file section
			}
			public void HTML() {
				_textile = Textile.TextileFormatter.FormatString(str.ToString());
				using (FileStream fs = File.Create(root + @"\receipt.html")) {
					AddText(fs, _textile);
				}
			}
			public void AddLine(string line) {
				str.WriteLine(line);
			}
			private static void AddText(FileStream fs, string value)
			{
				byte[] info = new UTF8Encoding(true).GetBytes(value);
				fs.Write(info, 0, info.Length);
			}
		}

		/* PROPERTIES */
		// TODO make private
		public static Git git = Git.Instance;
		public static OpenOCD openocd = OpenOCD.Instance;
		public static GDB gdb = GDB.Instance;
		public static MSBuild msbuild = MSBuild.Instance;
		public static Logic logic = Logic.Instance;
		public static Parser parser = Parser.Instance;
		public Feedback feedback;

		/* PUBLIC METHODS */
		public static void StartPerm() {
            Console.WriteLine("\n\nStarting OpenOCD ...");
            try
            {
                (new Thread(openocd.Start)).Start();
            }
            catch (Exception e)
            {
                Console.WriteLine("\n\n Start OpenOCD failed due to " + e.Message + "!!!");
            }
            
		}
		public void StartTemp(string root) {
            try
            {
                (new Thread(logic.Start)).Start();
            }
            catch (Exception e)
            {
                Console.WriteLine("\n\n Unable to start logic !!!");
            }
            // Disabling git initially, this needs to be modified
			//(new Thread(git.Start)).Start();
            try
            {
                (new Thread(msbuild.Start)).Start(root);
            }
            catch (Exception e)
            {
                Console.WriteLine("\n\nUnable to run msbuild on " + root + "!!!");
            }
            try
            {
                (new Thread(gdb.Start)).Start(root);
            }
            catch (Exception e)
            {
                Console.WriteLine("\n\nUnable to start gdb !!!");
            }
		}

		public void KillTemp() {
			git.Kill();
			msbuild.Kill();
			gdb.Kill();
		}
		public void SetFeedback(string root) {
			feedback = new Feedback(root);
			feedback.AddLine("Testing commit.");
		}
		public void Checkout(string root, string branch) {
			git.Checkout(root, branch);
		}
		public void Compile(string gdb_path_root, string proj_root, string proj) {
			msbuild.SetEnv(gdb_path_root);
			msbuild.Clean(proj_root, proj);
			msbuild.Build(proj_root, proj);
		}
		public void PrepareGDB(string axf, string entry) {
			gdb.Connect(axf);
			gdb.Load();
			gdb.ContinueTo(entry);
		}
	}/** SuiteInstance **/

	/********************
		TestInstance
	*********************/
	public class TestInstance {
		public TestInstance(SuiteInstance.Feedback feedback, string root, XmlElement config) {
			this.feedback = feedback;
			this.root = root;
			this.config = config;
			this.name = config["name"].InnerXml;
			this.entry = config["entry"].InnerXml;
			feedback.AddLine("Test: "+this.name);
		}

		/* OUTPUT */
		protected StringWriter stdOutput = new StringWriter();
		public StringWriter Output{ get { return stdOutput; }}
		protected StringWriter stdError = new StringWriter();
		public StringWriter Error{ get { return stdError; }}

		/* PROPERTIES */
		public SuiteInstance.Feedback feedback;
		public XmlElement config;
		public string root;
		public string name;
		public string entry;

		// TODO change to protected
		public GDB.Controller gdb = new GDB.Controller();
		public Logic.Controller logic = new Logic.Controller();
		public Parser.Controller parser = new Parser.Controller();

		/* OVERRIDE PUBLIC METHODS */
		// Setup() this test instance
		// called from _setup()
		protected bool Setup() {
			return true;
		}
		// Execute() this test, Logic runs after
		// called from _execute()
		protected bool Execute() {
			return true;
		}
		// alled after the Logic returns
		// called from _process()
		protected bool Process() {
			return true;
		}
		// Teardown() from user Setup()
		// called from _teardown()
		protected bool Teardown() {
			return true;
		}

		/* PUBLIC METHODS */
		public void _Setup() {
			if(!Request()) {
				stdError.WriteLine("request failed");
			}
			if(!Setup()) {
				stdError.WriteLine("user setup failed");
			}
		}
		public void _Execute() {
			if(!Execute()) {
				stdError.WriteLine("user execute failed");
			}
			if(!Process()) {
				stdError.WriteLine("user process failed");
				feedback.AddLine("test failed");
			} else {
				feedback.AddLine("test passed");
			}
		}
		public void _Teardown() {
			if(!Teardown()) {
				stdError.WriteLine("user teardown failed");
			}
			Relinquish();
		}

		/* PRIVATE METHODS */
		protected bool Request() {
			if(!gdb.Request()) {
				return false;
			}
			if(!logic.Request()) {
				return false;
			}
			if(!parser.Request()) {
				return false;
			}
			return true;
		}
		protected void Relinquish() {
			gdb.Relinquish();
			logic.Relinquish();
			parser.Relinquish();
		}
	}/** TestInstance **/
}/** SamTest **/
