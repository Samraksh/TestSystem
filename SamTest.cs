using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
// external
using SaleaeDeviceSdkDotNet;

/********************
	SamTest Namespace
*********************/
namespace SamTest {

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
		private string openocd_interface_cfg = @"interface\olimex-jtag-tiny.cfg";
		private string openocd_board_cfg = @"board\stm3210e_eval.cfg";

		/* PUBLIC METHODS */
		public bool Start() { //(string[] cfgs)
			Process process = new System.Diagnostics.Process();
			process.StartInfo.FileName = @"openocd.exe";
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
			return true;		
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
		private Regex ResultRegEx = new Regex(@"[\^]*");
		private Regex AsyncRegEx = new Regex(@"[\*\+=]*");
		private void StandardOutputHandler(object sendingProcess, DataReceivedEventArgs outLine) {
			stdOutput.WriteLine(outLine.Data);
			if(!String.IsNullOrEmpty(outLine.Data)) {
				// ResultRecordReceived
				if(ResultRegEx.IsMatch(outLine.Data)) {
					ParseResultRecord(@outLine.Data);
				}
				// AsyncRecordReceived
				if(AsyncRegEx.IsMatch(outLine.Data)) {
					ParseAsyncRecord(@outLine.Data);
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
		// TODO make private with gets{}
		private GdbCommandResult lastResult;
		private GdbEvent ev;
		private bool running;

		/* EVENT: ResultRecord */
		public class ResultRecordEventArgs: EventArgs {
			public ResultRecordEventArgs() {
			}
		}
		public event EventHandler<ResultRecordEventArgs> ResultRecordRecieved;
		private void OnResultRecordRecieved(ResultRecordEventArgs e){
			if(ResultRecordRecieved != null)
				ResultRecordRecieved(this, e);
		}
		/* EVENT: Connected */
		public event EventHandler<EventArgs> AsyncRecordRecieved;
		private void OnAsyncRecordRecieved(EventArgs e){
			if(AsyncRecordRecieved != null)
				AsyncRecordRecieved(this, e);
		}
		/* PUBLIC METHODS */
		public bool Start() {
			Process process = new System.Diagnostics.Process();
			process.StartInfo.FileName = @"arm-none-eabi-gdb.exe";
			process.StartInfo.Arguments = @"-quiet -fullname --interpreter=mi2";
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
			return true;
		}

		public GDBCommandResult RunCommand(string com) {
			input.WriteLine(com);
			new Thread(WaitCommand);
			return lastResult;
		}

		/* PRIVATE METHODS */
		public void ParseResultRecord(string s) {
			lastResult = new GdbCommandResult(s);
			running = (lastResult.Status == CommandStatus.Running);
		}
		public void ParseAsyncRecord(string s) {
			running = false;
			ev = new GdbEvent(s);
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
		private Regex buildRegEx = new Regex(@"(build complete|build failed)");
		private void StandardOutputHandler(object sendingProcess, DataReceivedEventArgs outLine) {
			stdOutput.WriteLine(outLine.Data);
			if(buildRegEx.IsMatch(outLine.Data)) {
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
		private static AutoResetEvent ARE_build = new AutoResetEvent(false);

		/* PUBLIC METHODS */
		public bool Start() {
			Process process = new System.Diagnostics.Process();
			process.StartInfo.FileName = @"cmd.exe";
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
			return true;
		}
		public bool SetEnv(string gdb_path_root) {
			input.WriteLine(@"setenv_gcc.cmd "+@gdb_path_root);
			return true;
		}
		public bool Clean(string root, string proj) {
			input.WriteLine(@"msbuild "+@root+@"\"+@proj+@" /target:clean");
			new Thread(WaitClean);
			return true;
		}
		public bool Build(string root, string proj) {
			input.WriteLine(@"msbuild "+@root+@"\"+@proj+@" /target:clean");
			new Thread(WaitBuild);
			return true;
		}

		/* PRIVATE METHODS */
		private void WaitClean() {
			ARE_build.WaitOne();
		}
		private void WaitBuild() {
			ARE_build.WaitOne();
		}
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
		private UInt32 mSampleRateHz = 4000000;
		private MLogic mLogic;
		//mLogic.IsStreaming();
		//mLogic.SampleRateHz = mSampleRateHz;
		//mLogic.SetOutput(mWriteValue);
		//mLogic.WriteStart();
		//mLogic.ReadStart();
		//mLogic.GetInput();
		//mLogic.Stop();

		/* EVENT: Connected */
		public event EventHandler<EventArgs> Connected;
		private void OnConnected(EventArgs e){
			if(Connected != null)
				Connected(this, e);
		}

		/* PUBLIC METHODS */
		public bool Start() {
			MSaleaeDevices devices = new MSaleaeDevices();
			devices.OnLogicConnect += new MSaleaeDevices.OnLogicConnectDelegate(LogicOnConnect);
			devices.OnDisconnect += new MSaleaeDevices.OnDisconnectDelegate(LogicOnDisconnect);
			devices.BeginConnect();
			stdOutput.WriteLine("Waiting for Logic connection.");
			return true;
		}
		public void AttachOnReadData(MLogic.OnReadDataDelegate fn) {
			mLogic.OnReadData += new MLogic.OnReadDataDelegate(fn);
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
			mLogic.OnReadData += new MLogic.OnReadDataDelegate(mLogic_OnReadData);
			mLogic.OnWriteData += new MLogic.OnWriteDataDelegate(mLogic_OnWriteData);
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
					EventArgs e = EventArgs.Empty;
					OnParsed(e);
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
		private string script = @"C:\SamTest\parser\parser.py";

		/* EVENT: Parsed */
		public event EventHandler<EventArgs> Parsed;
		private void OnParsed(EventArgs e){
			if(Parsed != null)
				Parsed(this, e);
		}
		/* EVENT: EventParsed */
		public delegate void EventParsedDelegate(string eventLine);
		public event EventParsedDelegate EventParsed;
		private void OnEventParsed(string eventLine){
			if(EventParsed != null)
				EventParsed(eventLine);
		}

		/* PUBLIC METHODS */
		// TODO RDP in c sharp
		public bool Start() {
			Process process = new Process();
			process.StartInfo.FileName = @"C:\Python32\python.exe";
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
			return true;
		}
		public void Parse(string path, string filename) {
			input.WriteLine(@script+' '+@path+' '+@filename);
		}
		public void AttachEventParsed(EventParsedDelegate fn) {
			EventParsed += new EventParsedDelegate(fn);
		}
		// TODO detach event

		/* PRIVATE METHODS */
	}/** Parser **/

	/********************
		SuiteInstance
	*********************/
	public class SuiteInstance {
		/* PROPERTIES */
		public OpenOCD openocd = OpenOCD.Instance;
		public GDB gdb = GDB.Instance;
		public MSBuild msbuild = MSBuild.Instance;
		public Logic logic = Logic.Instance;
		public Parser parser = Parser.Instance;

		/* PUBLIC METHODS */
		public bool StartAll() {
			if(!openocd.Start()) {
				return false;
			}
			if(!gdb.Start()) {
				return false;
			}
			if(!msbuild.Start()) {
				return false;
			}
			if(!logic.Start()) {
				return false;
			}
			if(!parser.Start()) {
				return false;
			}
			return true;
		}
		public bool Compile(string gdb_path_root, string root, string proj) {
			msbuild.SetEnv(gdb_path_root);
			msbuild.Clean(root, proj);
			msbuild.Build(root, proj);
			return true;
		}
	}/** SuiteInstance **/

	/********************
		TestInstance
	*********************/
	public class TestInstance {
		public TestInstance(string root, XmlElement config) {
			this.root = root;
			this.name = config["name"].Value;
			this.entry = config["entry"].Value;
		}

		/* OUTPUT */
		protected StringWriter stdOutput = new StringWriter();
		public StringWriter Output{ get { return stdOutput; }}
		protected StringWriter stdError = new StringWriter();
		public StringWriter Error{ get { return stdError; }}

		/* PROPERTIES */
		protected string root;
		protected string name;
		protected string entry;

		// TODO change to private
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
				// control was not granted
			}
			if(!Setup()) {
				// user setup returned false
			}
		}
		public void _Execute() {
			if(!Execute()) {
				// user execute failed
			}
			if(!Process()) {
				// user process failed
			}
		}
		public void _Teardown() {
			if(!Teardown()) {
				// user teardown failed
			}
			if(!Relinquish()) {
				// control was not returned
			}
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
		protected bool Relinquish() {
			gdb.Relinquish();
			logic.Relinquish();
			parser.Relinquish();
			return true;
		}
	}/** TestInstance **/
}/** SamTest **/
