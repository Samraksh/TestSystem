using System;
using System.Collections.Generic;
using System.IO;

namespace TestRig
{
    public class ReadParameters
    {            
            private StreamReader sr;
            private String line;

            public ReadParameters(string file, TestDescription currentTest)
            {
                try
                {                    
                    sr = new StreamReader(file);
                    line = sr.ReadLine();
                    while (line != null)
                    {
                        if (currentTest.testType == "C#")
                        {
                            if (line.Contains("=") == true)
                            {
                                string[] parsedLine = line.Split(' ');
                                string parameter = parsedLine[2].Trim();
                                string value = parsedLine[4];
                                value = value.Replace(';', ' ');
                                value = value.Trim();
                                if (parameter.Equals("testTimeout")) currentTest.testTimeout = int.Parse(value);
                                if (parameter.Equals("useLogic"))
                                {
                                    value = value.TrimStart('\"');
                                    value = value.TrimEnd('\"');
                                    currentTest.testUseLogic = value;
                                }
                                if (parameter.Equals("sampleTimeMs")) currentTest.testSampleTimeMs = int.Parse(value);
                                if (parameter.Equals("sampleFrequency")) currentTest.testSampleFrequency = int.Parse(value);
                                if (parameter.Equals("useTestScript")) currentTest.testUseScript = bool.Parse(value);
                                if (parameter.Equals("testScriptName"))
                                {
                                    value = value.TrimStart('\"');
                                    value = value.TrimEnd('\"');
                                    currentTest.testScriptName = value;
                                }
                                if (parameter.Equals("testScriptTimeoutMs")) currentTest.testScriptTimeoutMs = int.Parse(value);
                                if (parameter.Equals("useAnalysis"))
                                {
                                    value = value.TrimStart('\"');
                                    value = value.TrimEnd('\"');
                                    currentTest.testAnalysis = value;
                                }
                                if (parameter.Equals("analysisScriptName"))
                                {
                                    value = value.TrimStart('\"');
                                    value = value.TrimEnd('\"');
                                    currentTest.testAnalysisScriptName = value;
                                }
                                if (parameter.Equals("useResultsFile")) currentTest.testUseResultsFile = bool.Parse(value);
                                if (parameter.Equals("resultsFileName"))
                                {
                                    value = value.TrimStart('\"');
                                    value = value.TrimEnd('\"');
                                    currentTest.testResultsFileName = value;
                                }
                                if (parameter.Equals("useCOMPort")) currentTest.testUseCOM = bool.Parse(value);
                                if (parameter.Equals("forceCOM"))
                                {
                                    value = value.TrimStart('\"');
                                    value = value.TrimEnd('\"');
                                    currentTest.testForceCOM = value;
                                }
                                if (parameter.Equals("COMParameters"))
                                {
                                    value = value.TrimStart('\"');
                                    value = value.TrimEnd('\"');
                                    currentTest.testCOMParameters = value;
                                }
                            }                    
                        }
                        else
                        {
                            line = line.Replace("\t", " ");
                            if (line.Contains("define") == true)
                            {
                                string[] parsedLine = line.Split(' ');
                                string parameter = parsedLine[1].Trim();
                                string value = parsedLine[2];
                                value = value.Replace(';', ' ');
                                value = value.Trim();
                                if (parameter.Equals("testTimeout")) currentTest.testTimeout = int.Parse(value);
                                if (parameter.Equals("useLogic"))
                                {
                                    value = value.TrimStart('\"');
                                    value = value.TrimEnd('\"');
                                    currentTest.testUseLogic = value;
                                }
                                if (parameter.Equals("sampleTimeMs")) currentTest.testSampleTimeMs = int.Parse(value);
                                if (parameter.Equals("sampleFrequency")) currentTest.testSampleFrequency = int.Parse(value);
                                if (parameter.Equals("useTestScript")) currentTest.testUseScript = bool.Parse(value);
                                if (parameter.Equals("testScriptName"))
                                {
                                    value = value.TrimStart('\"');
                                    value = value.TrimEnd('\"');
                                    currentTest.testScriptName = value;
                                }
                                if (parameter.Equals("testScriptTimeoutMs")) currentTest.testScriptTimeoutMs = int.Parse(value);
                                if (parameter.Equals("useAnalysis"))
                                {
                                    value = value.TrimStart('\"');
                                    value = value.TrimEnd('\"');
                                    currentTest.testAnalysis = value;
                                }
                                if (parameter.Equals("analysisScriptName"))
                                {
                                    value = value.TrimStart('\"');
                                    value = value.TrimEnd('\"');
                                    currentTest.testAnalysisScriptName = value;
                                }
                                if (parameter.Equals("useResultsFile")) currentTest.testUseResultsFile = bool.Parse(value);
                                if (parameter.Equals("resultsFileName"))
                                {
                                    value = value.TrimStart('\"');
                                    value = value.TrimEnd('\"');
                                    currentTest.testResultsFileName = value;
                                }
                                if (parameter.Equals("useCOMPort")) currentTest.testUseCOM = bool.Parse(value);
                                if (parameter.Equals("forceCOM"))
                                {
                                    value = value.TrimStart('\"');
                                    value = value.TrimEnd('\"');
                                    currentTest.testForceCOM = value;
                                }
                                if (parameter.Equals("COMParameters"))
                                {
                                    value = value.TrimStart('\"');
                                    value = value.TrimEnd('\"');
                                    currentTest.testCOMParameters = value;
                                }
                            }   
                        }
                        line = sr.ReadLine();
                    }
                    sr.Close();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("read file line: " + ex.ToString());
                }
            }
    }
}
