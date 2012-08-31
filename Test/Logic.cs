using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace SamTest{

	public class Logic{

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
		
	}


}



