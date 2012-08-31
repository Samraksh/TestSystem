using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
// external
// using SaleaeDeviceSdkDotNet;
using SamTest;

public class GPIO: SamTest.StandardTest {
	public GPIO(SamTest.SuiteInstance.Feedback feedback, string root, XmlElement config) : base(feedback, root, config) {
		// this is the class initializer
		// an instance is made for each test
	}
	public new bool Setup() {
		// any kind of setup (optional)
		return true;
	}
	public new bool Exectue() {
		// execute a software test or custom logic test (optional)
		// gdb.handle.FUNCTION
		return true;
	}
	public new bool Process() {
		// check to see if test was successful (optional)
		// write info to feedback file
		feedback.AddLine("User processing")
		// look at the results stored in the ETlist
		//   add the times to a separte file
		StringWriter timesStr = new StringWriter();
		for(int i; i < ETlist.Count; i++) {
			EventTime temp = ETlist.RemoveAt(0); // remove first
			string name = temp.Name;
			int time = temp.time;
			timesStr.WriteLine(time);
		}
		// convert string to file and append to feedback
		feedback.AddAsFile("times.txt", timesStr.ToString());
		// write info to feedback file
		feedback.AddLine("User feedback");
		// it might be easier to work with results in array form:
		EventTime[] ETarray = ETlist.ToArray();
		foreach(EventTime j in ETarray) {
			string name = j.Name;
			int time = j.time;
		}
		// if no errors return true
		return true;
	}
	public new bool Teardown() {
		// teardown what you setup (optional)
		return true;
	}
}