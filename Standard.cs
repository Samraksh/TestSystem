using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;
// Pins type
using Pins = System.UInt32;

/********************
	SamTest Namespace
*********************/
namespace SamTest {
	public class StandardTest: TestInstance {
		public StandardTest(string root, XmlElement config) : base(root, config) {
			this.edf = config["edf"].Value;
			this.hkp = config["hkp"].Value;
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
		}/** EventTime **/

		/* PROPERTIES */
		private string edf;
		private string hkp;

		private UInt64 offset;
		private List<EventRep> ERlist = new List<EventRep>();
		public List<EventRep> ERs{ get{ return ERlist; }}
		private List<EventTime> ETlist = new List<EventTime>();
		public List<EventTime> ETs{ get{ return ETlist; }}

		private bool firstCall = true;
		private Pins last;

		public new void _Setup() {
			if(!Request()) {
				// control was not granted
			}
			if(!Setup()) {
				// user setup returned false
			}
		}
		public new void _Execute() {
			if(!Execute()) {
				// user execute failed
			}
			if(!Process()) {
				// user process failed
			}
		}
		public new void _Teardown() {
			if(!Teardown()) {
				// user teardown failed
			}
			if(!Relinquish()) {
				// control was not returned
			}
		}

		public void Parse() {
			if(parser.haveControl) {
				stdOutput.WriteLine("Parsing.");
				parser.handle.Parse(root, edf);
			}
		}
		public void Start() {
			if(logic.haveControl) {
				stdOutput.WriteLine("Starting.");
				logic.handle.ReadStart();
			}
		}
		public void Stop() {
			if(logic.haveControl) {
				stdOutput.WriteLine("Stopping.");
				logic.handle.ReadStop();
			}
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
		private void EventParsedEvent(string eventLine) {
			ERlist.Add(new EventRep(eventLine.Split('\t')));
		}
	}
}

public class GPIO: SamTest.StandardTest {
	public GPIO(string root, XmlElement config) : base(root, config) {
	}
}
