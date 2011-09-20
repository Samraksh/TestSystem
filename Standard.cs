using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Xml;
// Pins type
using Pins = System.UInt32;

/********************
	SamTest Namespace
*********************/
namespace SamTest {
	public class StandardTest: TestInstance {
		public StandardTest(SamTest.SuiteInstance.Feedback feedback, string root, XmlElement config) : base(feedback, root, config) {
			this.edf = config["edf"].InnerXml;
			this.hkp = config["hkp"].InnerXml;
		}

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
		}/** EventRep **/

		/********************
			EventTime
		*********************/
		public class EventTime {
			/* CONSTRUCTORS */
			public EventTime(string Name, UInt64 offset) {
				this.Name = Name;
				this.offset = offset;
				// TODO: computer clientOffset based on client rate
				this.time = offset * Logic.RateInv; // (sample) * (second/sample)
			}

			/* PROPERTIES */
			public string Name;
			public UInt64 offset;
			public UInt64 clientOffset;
			public UInt64 time;
		}/** EventTime **/

		/* PROPERTIES */
		public string edf;
		public string hkp;

		protected UInt64 offset;
		protected List<EventRep> ERlist = new List<EventRep>();
		public List<EventRep> ERs{ get{ return ERlist; }}
		protected List<EventTime> ETlist = new List<EventTime>();
		public List<EventTime> ETs{ get{ return ETlist; }}

		protected bool firstCall = true;
		protected Pins last;

		// internal setup
		public new void _Setup() {
			// request handle control
			if(!Request()) {
				stdError.WriteLine("request failed");
			}
			// attach the callbacks
			logic.handle.AttachOnReadData(this.OnReadData);
			parser.handle.AttachEventParsed(this.EventParsed);
			// parse the edf file
			(new Thread(parser.handle.Parse)).Start(root+@" "+edf);
			// move program execution to test entry point
			gdb.handle.JumpTo(entry);
			// call the user setup
			if(!Setup()) {
				stdError.WriteLine("user setup failed");
			}
		}
		// internal execute
		public new void _Execute() {
			// call the user execute
			if(!Execute()) {
				stdError.WriteLine("user execute failed");
			}
			// run the logic
			logic.handle.ReadStart();
			// start the GDB program and run until finished
			gdb.handle.Execute();
			// stop the logic
			logic.handle.ReadStop();
			// call the user process
			if(!Process()) {
				stdError.WriteLine("user process failed");
			} else {
				// report test success
			}
		}
		// internal teardown
		public new void _Teardown() {
			// call the user teardown
			if(!Teardown()) {
				stdError.WriteLine("user teardown failed");
			}
			// detach callbacks
			logic.handle.DetachOnReadData(OnReadData);
			parser.handle.DetachEventParsed(EventParsed);
			// release the handles
			Relinquish();
		}
		// callback
		protected void OnReadData(ulong device_id, byte[] data){
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
				if (Tc != (Pins)0){
					// On each event
					for(int eI = 0; eI < ERlist.Count; eI++){
						EventRep Ec = ERlist[eI];
						Tcp = Tc & Ec.X;
						if(Tcp != (Pins)0) {
							if (((Sc & Ec.X) == Ec.S) && ((Tcp & Ec.T) == Ec.T)) {
								ETlist.Add(new EventTime(Ec.Name, offset));
							}
						}
					}
				}
				last = Sc;
			}
		}
		// callback
		protected void EventParsed(string eventLine) {
			ERlist.Add(new EventRep(eventLine.Split('\t')));
		}
	}
}
