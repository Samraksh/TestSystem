using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
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
		public static OpenOCD Instance{
			get {
				return instance; 
			}
		}
	}

	/********************
		GDB - Singleton
	*********************/
	public sealed class GDB {
		private static readonly GDB instance = new GDB();
		/* CONSTRUCTOR */
		private GDB(){
			
		}
		public static GDB Instance{
			get {
				return instance; 
			}
		}
	}

	/********************
		MSBuild - Singleton
	*********************/
	public sealed class MSBuild {
		private static readonly MSBuild instance = new MSBuild();
		/* CONSTRUCTOR */
		private MSBuild(){
			
		}
		public static MSBuild Instance{
			get {
				return instance; 
			}
		}
	}

	/********************
		Logic - Singleton
	*********************/
	public sealed class Logic {
		private static readonly Logic instance = new OpenOCD();
		/* CONSTRUCTOR */
		private Logic(){
			MSaleaeDevices devices = new MSaleaeDevices();
			devices.OnLogicConnect += new MSaleaeDevices.OnLogicConnectDelegate(LogicOnConnect);
			devices.OnDisconnect += new MSaleaeDevices.OnDisconnectDelegate(LogicOnDisconnect);
			devices.BeginConnect();
			Console.WriteLine("Waiting for Logic connection.");
		}
		public static Logic Instance{ get { return instance; }}

		/* PROPERTIES */
		private UInt32 mSampleRateHz = 4000000;
		private MLogic mLogic;

		/* EVENT: Connected */
		public event EventHandler<EventArgs> Connected;
		protected virtual void OnConnected(EventArgs e){
			if(Connected != null)
				Connected(this, e);
		}

		/* PUBLIC METHODS */

		/* PRIVATE METHODS */
		private void LogicOnDisconnect(ulong device_id){
			Console.WriteLine("Logic with id {0} disconnected.", device_id);
			if (mLogic != null)
					mLogic = null;
		}
		private void LogicOnConnect(ulong device_id, MLogic logic){
			Console.WriteLine("Logic with id {0} connected.", device_id);
			mLogic = logic;
			mLogic.OnReadData += new MLogic.OnReadDataDelegate(mLogic_OnReadData);
			mLogic.OnWriteData += new MLogic.OnWriteDataDelegate(mLogic_OnWriteData);
			mLogic.OnError += new MLogic.OnErrorDelegate(mLogic_OnError);
			mLogic.SampleRateHz = mSampleRateHz;
			// invoke the Connected event
			OnConnected(EventArgs.Empty);

			//mLogic.IsStreaming();
			//mLogic.SampleRateHz = mSampleRateHz;
			//mLogic.SetOutput(mWriteValue);
			//mLogic.WriteStart();
			//mLogic.ReadStart();
			//mLogic.GetInput();
			//mLogic.Stop();
		}
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
					Tcp = Tc & Ec.getX();
					if (Tcp != (Pins)0){
						if (((Sc & Ec.getX()) == Ec.getS()) && ((Tcp & Ec.getT()) == Ec.getT())){
							ETlist.Add(new EventTime(Ec, offset));
						}
					}
				}
				last = Sc;
			}
		}
		private void mLogic_OnWriteData(ulong device_id, byte[] data){
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
		private Parser(){
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
			StreamWriter input = process.StandardInput;
			// StandardOutput stream
			process.OutputDataReceived += new DataReceivedEventHandler(StandardOutputHandler);
			process.BeginOutputReadLine();
			// StandardError stream
			process.ErrorDataReceived += new DataReceivedEventHandler(StandardErrorHandler);
			process.BeginErrorReadLine();
		}
		public static Parser Instance{ get { return instance; }}

		private TextWriter ParseOutput = new TextWriter();
		public TextWriter Output{ get { return ParseOutput; }}
		private TextWriter ParseError = new TextWriter();
		public TextWriter Error{ get { return ParseOutput; }}

		private void StandardOutputHandler(object sendingProcess, DataReceivedEventArgs outLine){
			ParseOutput.WriteLine(outLine);
			if(!String.IsNullOrEmpty(outLine.Data)) {
				if(outLine.Data == "Parse complete."){
					// Invoke the Parsed event
					ParsedEventArgs e = new ParsedEventArgs(this.ERlist);
					OnParsed(e);
					return;
				}
				// Add to instance ERList
				ERlist.Add(new EventRep(outLine.Data.Split('\t')));
			}
		}

		private void StandardErrorHandler(object sendingProcess, DataReceivedEventArgs errLine){
			ParseError.WriteLine(errLine);
		}
	}

	/********************
		TestInstance
	*********************/
	public class TestInstance {

		public class EventRep {
			public EventRep(string name, Pins X, Pins T, Pins S) {
				this.name = name;
				this.X = X;
				this.T = T;
				this.S = S;
			}
			public EventRep(string name, string X, string T, string S) {
				this.name = name;
				this.X = Convert.ToUInt32(X);
				this.T = Convert.ToUInt32(T);
				this.S = Convert.ToUInt32(S);
			}
			public EventRep(string[] testevent) {
				this.name = testevent[0];
				this.X = Convert.ToUInt32(testevent[1]);
				this.T = Convert.ToUInt32(testevent[2]);
				this.S = Convert.ToUInt32(testevent[3]);
			}

			public String name;
			public Pins X;
			public Pins T;
			public Pins S;

			// Access methods
			public String getName(){
				return this.name;
			}
			public Pins getX(){
				return this.X;
			}
			public void setX(Pins x){
				this.X = x;
			}
			public Pins getT(){
				return this.T;
			}
			public void setT(Pins t){
				this.T = t;
			}
			public Pins getS(){
				return this.S;
			}
			public void setS(Pins s){
				this.S = s;
			}	
		}

		public class EventTime {
			public EventTime(EventRep ER, UInt64 offset) {
				this.ER = ER;
				this.offset = offset;
				// TODO: computer clientOffset and time
			}

			public EventRep ER;
			public UInt64 offset;
			public UInt64 clientOffset;
			public UInt64 time;
		}

		private bool canRun = true;
		private bool firstCall = true;
		private Pins last;
		private UInt64 offset;
		private List<EventRep> ERlist = new List<EventRep>();
		private List<EventTime> ETlist = new List<EventTime>();

		// EVENT: Parsed
		public class ParsedEventArgs: EventArgs {
			public ParsedEventArgs(List<EventRep> ERlist) {
				this.ERlist = ERlist;
			}
			public List<EventRep> ERlist;
		}
		public event EventHandler<ParsedEventArgs> Parsed;
		protected virtual void OnParsed(ParsedEventArgs e){
			if(Parsed != null)
				Parsed(this, e);
		}
		// EVENT: Stopped
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

		public void Start(){
			if(canRun) {
				mLogic.ReadStart();
				canRun = false;
			} else {
				Console.WriteLine("This TestInstance has already run.");
			}
		}

		public void Stop(){
			Console.WriteLine("Stopping.");
			mLogic.Stop();
			// Invoke the Stopped event
			StoppedEventArgs e = new StoppedEventArgs(this.ETlist);
			OnStopped(e);
		}
	}
}