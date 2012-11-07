using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using LabSharp;

namespace TestRig
{    	
    class Matlab
    {
        private Engine engine;
        public MainWindow mainHandle;
        public TestReceipt testResults;

        public Matlab(MainWindow passedHandle, TestReceipt results)
        {
            mainHandle = passedHandle;
            testResults = results;

            engine = Engine.Open();
            if (engine.NativeObject == null)
                System.Diagnostics.Debug.WriteLine("Null Reference Exception: Failed to Initialize Engine");

            engine.Visible = false;
        }

        public bool matlabRunScript(string script, string dataFileName, TestDescription  currentTest)
        {
            try
            {
                engine.Eval("clear all");
                engine.Eval("cd " + script);

                engine.SetVariable("result", "FAIL");
                engine.SetVariable("accuracy", 0);
                engine.SetVariable("resultParameter1", "");
                engine.SetVariable("resultParameter2", "");
                engine.SetVariable("resultParameter3", "");
                engine.SetVariable("resultParameter4", "");
                engine.SetVariable("resultParameter5", "");
                engine.SetVariable("dataFileName", dataFileName);

                // stripping .m from the matlab script name
                string matlabScriptName = currentTest.testMatlabScriptName.Replace(".m", "");
                engine.Eval(matlabScriptName);
                

                if (engine.GetVariable<string>("result") == "PASS")
                {
                    System.Diagnostics.Debug.WriteLine("Test passed.");
                    testResults.testPass = true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Test failed.");
                    testResults.testPass = false;
                }

                testResults.testAccuracy = engine.GetVariable<double>("accuracy");

                testResults.testReturnParameter1 = engine.GetVariable<string>("resultParameter1");
                testResults.testReturnParameter2 = engine.GetVariable<string>("resultParameter2");
                testResults.testReturnParameter3 = engine.GetVariable<string>("resultParameter3");
                testResults.testReturnParameter4 = engine.GetVariable<string>("resultParameter4");
                testResults.testReturnParameter5 = engine.GetVariable<string>("resultParameter5");

                System.Diagnostics.Debug.WriteLine("Analysis result parameter1 : " + engine.GetVariable<string>("resultParameter1"));
                System.Diagnostics.Debug.WriteLine("Analysis result parameter2 : " + engine.GetVariable<string>("resultParameter2"));
                System.Diagnostics.Debug.WriteLine("Analysis result parameter3 : " + engine.GetVariable<string>("resultParameter3"));
                System.Diagnostics.Debug.WriteLine("Analysis result parameter4 : " + engine.GetVariable<string>("resultParameter4"));
                System.Diagnostics.Debug.WriteLine("Analysis result parameter5 : " + engine.GetVariable<string>("resultParameter5"));

                testResults.testComplete = true;
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Matlab run fail: " + ex.Message);
                testResults.testComplete = false;
                return false;
            }
        }

        public void Kill()
        {
            try
            {
                engine.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Matlab close fail: " + ex.Message);
            }
        }
    }
}
