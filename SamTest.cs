using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
// external
using SaleaeDeviceSdkDotNet;
// Pins type
using Pins = System.UInt32;

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
		public static OpenOCD Instance{ get { return instance; }}
		/* PROCESS HANDLERS */
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
		private string openocd_interface_cfg = "olimex-jtag-tiny.cfg";
		private string openocd_board_cfg = "stm3210e_eval.cfg";

		/* PUBLIC METHODS */
		public bool Start() {
			Process process = new System.Diagnostics.Process();
			process.StartInfo.FileName = @"openocd.exe";
			process.StartInfo.Arguments = @"-f interface\"+openocd_interface_cfg+@" -f board\"+openocd_board_cfg;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			// Streams
			process.StartInfo.RedirectStandardInput = true;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			// Start
			process.Start();
			// StandardInput stream
			StreamWriter input = process.StandardInput;
			// StandardOutput stream
			process.OutputDataReceived += new DataReceivedEventHandler(StandardOutputHandler);
			process.BeginOutputReadLine();
			// StandardError stream
			process.ErrorDataReceived += new DataReceivedEventHandler(StandardErrorHandler);
			process.BeginErrorReadLine();	
			return true;		
		}
		/* PRIVATE METHODS */
	}

	/********************
		GDB - Singleton
	*********************/
	public sealed class GDB {
		private static readonly GDB instance = new GDB();
		/* CONSTRUCTOR */
		private GDB(){
		}
		public static GDB Instance{ get { return instance; }}
		/* PROCESS HANDLERS */
		private void StandardOutputHandler(object sendingProcess, DataReceivedEventArgs outLine) {
			stdOutput.WriteLine(outLine.Data);
			if(!String.IsNullOrEmpty(outLine.Data)) {
				// ResultRecordReceived
				if(ResultRegEx.IsMatch(outLine.Data)) {
					//ParseResultRecord(outLine.Data);
				}
				// AsyncRecordReceived
				if(AsyncRegEx.IsMatch(outLine.Data)) {
					//ParseAsyncRecord(outLine.Data);
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

		/* PROPERTIES */
		Regex ResultRegEx = new Regex(@"[\^]*");
		Regex AsyncRegEx = new Regex(@"[\*\+=]*");

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
			StreamWriter input = process.StandardInput;
			// StandardOutput stream
			process.OutputDataReceived += new DataReceivedEventHandler(StandardOutputHandler);
			process.BeginOutputReadLine();
			// StandardError stream
			process.ErrorDataReceived += new DataReceivedEventHandler(StandardErrorHandler);
			process.BeginErrorReadLine();
			return true;
		}

		/* PRIVATE METHODS */
		/*
		private void ParseResultRecord(string s) {
			XmlDocument resultRecord = new XmlDocument();
			ParseRecord(out resultRecord, out s);
		}
		private void ParseAsyncRecord(string s) {
			XmlDocument asyncRecord = new XmlDocument();
			ParseRecord(out asyncRecord, out s);
		}
		private XmlElement ParseRecord(out XmlDocument doc, out string s) {
			XmlElement rootElement = doc.CreateElement();
			//s = s.TrimStart('^')
			XmlElement resultClass = ParseResultClass(out s);
			while(!String.IsNullOrEmpty(s)) {
				if(s.StartsWith(',')) { s = s.TrimStart(','); }
				XmlElement result = ParseResult(out s);
				resultClass.AppendChild(result);
			}
			rootElement.AppendChild(resultClass);
			return rootElement;
		}
		private XmlElement ParseResultClass(out XmlDocument doc, out string s) {
			XmlElement resultClass = new XmlElement();
    	string[] split = s.Split(",", 2) // resultclass, s
    	resultClass = split[0];
    	s = split[1];
    	return resultClass;
		}

		private XmlElement ParseResult(out XmlDocument doc, out string s) {
			XmlElement result = new XmlElement();
	    $result = @{} 
	    string[] split = s.Split("=", 2);
	    string variable = split[0];
	    s = split[1];
	    string value = ParseValue(out s);
	    $result += @{$variable=$value}
	    $result
	    $string
		}

		Function Parse-Value-GDB {
		    param($string)
		    if($string.StartsWith('{')) {
		        $value = @{}
		        $string = $string.TrimStart('{')
		        if($string.StartsWith('}')) { $null; $string.TrimStart('}'); return }
		        do {
		            if($string.StartsWith(',')) { $string = $string.TrimStart(',');  }
		            $result, $string = Parse-Result-GDB $string
		            $value += $result
		        } while($string.StartsWith(','))
		        $value
		        $string.TrimStart('}')
		    } elseif($string.StartsWith("[")) {
		        $value = @{}
		        $string = $string.TrimStart('[')
		        if($string.StartsWith(']')) { $null; $string.TrimStart(']'); return }
		        do {
		            if($string.StartsWith(',')) { $string = $string.TrimStart(',');  }
		            $result, $string = Parse-Value-GDB $string
		            $value += $result
		        } while($string.StartsWith(','))
		        $value
		        $string.TrimStart(']')
		    } elseif($string.StartsWith('"')) {
		        $string = $string.TrimStart('"')
		        $string.Split('"', 2)
		    } else {
		        Parse-Result-GDB $string
		    }
		}
			*/
	}

	/********************
		MSBuild - Singleton
	*********************/
	public sealed class MSBuild {
		private static readonly MSBuild instance = new MSBuild();
		/* CONSTRUCTOR */
		private MSBuild(){
		}
		public static MSBuild Instance{ get { return instance; }}

		/* OUTPUT */
		private StringWriter stdOutput = new StringWriter();
		public StringWriter Output{ get { return stdOutput; }}
		private StringWriter stdError = new StringWriter();
		public StringWriter Error{ get { return stdError; }}

		/* PROPERTIES */
		/* PUBLIC METHODS */
		/* PRIVATE METHODS */
	}

	/********************
		Logic - Singleton
	*********************/
	public sealed class Logic {
		private static readonly Logic instance = new Logic();
		/* CONSTRUCTOR */
		private Logic(){
		}
		public static Logic Instance{ get { return instance; }}

		/* OUTPUT */
		private StringWriter stdOutput = new StringWriter();
		public StringWriter Output{ get { return stdOutput; }}
		private StringWriter stdError = new StringWriter();
		public StringWriter Error{ get { return stdError; }}

		/********************
			LogicController
		*********************/
		public class LogicController {
			/* CONTROLLER PROPERTIES */
			private bool m_haveControl = false;
			public bool haveControl{ get { return m_haveControl; }}
			private Logic logic = null;
			public Logic handle{ get { return logic; }}

			/* CONTROLLER METHODS */
			public bool RequestControl() {
				m_haveControl = Logic.RequestControl(this, out logic);
				return m_haveControl;
			}
			public bool ReturnControl() {
				m_haveControl = Logic.ReturnControl(this, out logic);
				return m_haveControl;
			}
		}

		/* PROPERTIES */
		private UInt32 mSampleRateHz = 4000000;
		private MLogic mLogic;
		private static LogicController nullController = new LogicController();
		private static LogicController currentController = nullController;
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

		/* INTERNAL METHODS */
		internal static bool RequestControl(LogicController r, out Logic rInstance) {
			//if(object.ReferenceEquals(currentController, nullController)) {
				if(currentController == nullController) {
				currentController = r;
				rInstance = instance;
				return true;
			} else {
				rInstance = null;
				return false;
			}
		}
		internal static bool ReturnControl(LogicController r, out Logic rInstance) {
			if(object.ReferenceEquals(currentController, r)) {
				currentController = nullController;
			}
			rInstance = null;
			return false;
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
		public void AttachOnReadData(MLogic.OnReadDataDelegate ReadDelegate) {
			mLogic.OnReadData += ReadDelegate;
		}
		public bool SetSampleRate(UInt32 mSampleRateHz) {
			if(!isRunning()) {
				mLogic.SampleRateHz = mSampleRateHz;
				return true;
			} else {
				// can't - logic is running
				return false;
			}
		}
		public bool Start() {
			if(!isRunning()) {
				mLogic.ReadStart();
				return true;
			} else {
				// can't - logic is running
				return false;
			}
		}
		public bool Stop() {
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
	}

	/********************
		Parser - Singleton
	*********************/
	public sealed class Parser {
		private static readonly Parser instance = new Parser();
		/* CONSTRUCTOR */
		private Parser() {
		}
		public static Parser Instance{ get { return instance; }}
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
				// Add to instance ERList
				m_ERlist.Add(new TestInstance.EventRep(outLine.Data.Split('\t')));
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
			ParserController
		*********************/
		public class ParserController {
			/* CONTROLLER PROPERTIES */
			private bool m_haveControl = false;
			public bool haveControl{ get { return m_haveControl; }}
			private Parser parser = null;
			public Parser handle{ get { return parser; }}

			/* CONTROLLER METHODS */
			public bool RequestControl() {
				m_haveControl = Parser.RequestControl(this, out parser);
				return m_haveControl;
			}
			public bool ReturnControl() {
				m_haveControl = Parser.ReturnControl(this, out parser);
				return m_haveControl;
			}
		}

		/* PROPERTIES */
		private string script = @"C:\SamTest\parser\parser.py";
		private static ParserController nullController = new ParserController();
		private static ParserController currentController = nullController;
		private List<TestInstance.EventRep> m_ERlist = new List<TestInstance.EventRep>();
		public List<TestInstance.EventRep> ERlist{ get {return m_ERlist; }}

		/* EVENT: Parsed */
		public event EventHandler<EventArgs> Parsed;
		private void OnParsed(EventArgs e){
			if(Parsed != null)
				Parsed(this, e);
		}

		/* INTERNAL METHODS */
		internal static bool RequestControl(ParserController r, out Parser rInstance) {
			//if(object.ReferenceEquals(currentController, nullController)) {
				if(currentController == nullController) {
				currentController = r;
				rInstance = instance;
				return true;
			} else {
				rInstance = null;
				return false;
			}
		}
		internal static bool ReturnControl(ParserController r, out Parser rInstance) {
			if(object.ReferenceEquals(currentController, r)) {
				currentController = nullController;
			}
			rInstance = null;
			return false;
		}

		/* PUBLIC METHODS */
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

		/* PRIVATE METHODS */
	}

	/********************
		TestInstance
	*********************/
	public class TestInstance {
		public TestInstance(string root, XmlElement config) {
			this.root = root;
			this.name = config["name"].Value;
			this.entry = config["entryPoint"].Value;
			this.edf = config["edf"].Value;
			this.hkp = config["hkp"].Value;
		}

		/* OUTPUT */
		private StringWriter stdOutput = new StringWriter();
		public StringWriter Output{ get { return stdOutput; }}
		private StringWriter stdError = new StringWriter();
		public StringWriter Error{ get { return stdError; }}

		/********************
			EventRep
		*********************/
		public class EventRep {
			/* CONSTRUCTORS */
			public EventRep(string Name, Pins X, Pins T, Pins S) {
				this.iName = Name;
				this.iX = X;
				this.iT = T;
				this.iS = S;
			}
			public EventRep(string Name, string X, string T, string S) {
				this.iName = Name;
				this.iX = Convert.ToUInt32(X);
				this.iT = Convert.ToUInt32(T);
				this.iS = Convert.ToUInt32(S);
			}
			public EventRep(string[] testevent) {
				this.iName = testevent[0];
				this.iX = Convert.ToUInt32(testevent[1]);
				this.iT = Convert.ToUInt32(testevent[2]);
				this.iS = Convert.ToUInt32(testevent[3]);
			}

			/* PROPERTIES */
			private String iName;
			private Pins iX;
			private Pins iT;
			private Pins iS;

			/* ACCESS METHODS */
			public String Name{
				get { return this.iName; }
			}
			public Pins X{
				get { return this.iX; }
			}
			public Pins T{
				get { return this.iT; }
			}
			public Pins S{
				get { return this.iS; }
			}
		}

		/********************
			EventTime
		*********************/
		public class EventTime {
			/* CONSTRUCTORS */
			public EventTime(EventRep ER, UInt64 offset) {
				this.ER = ER;
				this.offset = offset;
				// TODO: computer clientOffset and time
			}

			/* PROPERTIES */
			public EventRep ER;
			public UInt64 offset;
			public UInt64 clientOffset;
			public UInt64 time;
		}

		/* PROPERTIES */
		private string root;
		private string name;
		private string entry;
		private string edf;
		private string hkp;

		private UInt64 offset;
		private List<EventRep> ERlist = new List<EventRep>();
		public List<EventRep> ERs{ get{ return ERlist; }}
		private List<EventTime> ETlist = new List<EventTime>();
		public List<EventTime> ETs{ get{ return ETlist; }}

		private bool firstCall = true;
		private Pins last;

		// TODO change to private
		public Logic.LogicController cLogic = new Logic.LogicController();
		public Parser.ParserController cParser = new Parser.ParserController();

		/* EVENT: Stopped */
		public class StoppedEventArgs: EventArgs {
			public StoppedEventArgs(List<EventTime> ETlist) {
				this.ETlist = ETlist;
			}
			public List<EventTime> ETlist;
		}
		public event EventHandler<StoppedEventArgs> Stopped;
		protected virtual void OnStopped(StoppedEventArgs e){
			if(Stopped != null)
				Stopped(this, e);
		}

		/* PUBLIC METHODS */
		public bool Activate() {
			if(cLogic.RequestControl() && cParser.RequestControl()) {
				return true;
			}
			return false;
		}
		public void DeActivate() {
			cLogic.ReturnControl();
			cParser.ReturnControl();
		}
		public void Parse() {
			if(cParser.haveControl) {
				stdOutput.WriteLine("Parsing.");
				cParser.handle.Parse(root, edf);
			}
		}
		public void Start() {
			if(cLogic.haveControl) {
				stdOutput.WriteLine("Starting.");
				cLogic.handle.Start();
			}
		}
		public void Stop() {
			if(cLogic.haveControl) {
				stdOutput.WriteLine("Stopping.");
				cLogic.handle.Stop();
				// Invoke the Stopped event
				StoppedEventArgs e = new StoppedEventArgs(this.ETlist);
				OnStopped(e);
			}
		}

		/* PRIVATE METHODS */
		private void mLogic_OnReadData(ulong device_id, byte[] data){
			Pins Tc = 0;
			Pins Sc = 0;
			Pins Tcp = 0;
			if(firstCall){
				last = (Pins)data[0];
				firstCall = false;
			}
			// On each byte of data
			for(int dI = 1; dI < data.Length; dI++){
				offset++;
				Sc = (Pins)data[dI];
				Tc = last ^ Sc;
				// On each event
				for(int eI = 0; eI < ERlist.Count; eI++){
					EventRep Ec = ERlist[eI];
					Tcp = Tc & Ec.X;
					if (Tcp != (Pins)0){
						if (((Sc & Ec.X) == Ec.S) && ((Tcp & Ec.T) == Ec.T)) {
							ETlist.Add(new EventTime(Ec, offset));
						}
					}
				}
				last = Sc;
			}
		}
	}
}
