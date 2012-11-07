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
                        if (currentTest.testType == "C")
                        {
                        }
                        else
                        {
                            if (line.Contains("=") == true)
                            {
                                string[] parsedLine = line.Split(' ');
                                string parameter = parsedLine[2].Trim();
                                string value = parsedLine[4];
                                value = value.Replace(';',' ');
                                value = value.Trim();
                                if (parameter.Equals("useLogic")) currentTest.testUseLogic = bool.Parse(value);
                                if (parameter.Equals("sampleTimeMs")) currentTest.testSampleTimeMs = int.Parse(value);
                                if (parameter.Equals("sampleFrequency")) currentTest.testSampleFrequency = int.Parse(value);
                                if (parameter.Equals("useExecutable")) currentTest.testUseExecutable = bool.Parse(value);
                                if (parameter.Equals("executableName"))
                                {
                                    value = value.TrimStart('\"');
                                    value = value.TrimEnd('\"');
                                    currentTest.testExecutableName = value;
                                }
                                if (parameter.Equals("executableTimeoutMs")) currentTest.testExecutableTimeoutMs = int.Parse(value);
                                if (parameter.Equals("useMatlabAnalysis")) currentTest.testMatlabAnalysis = bool.Parse(value);
                                if (parameter.Equals("matlabScriptName"))
                                {
                                    value = value.TrimStart('\"');
                                    value = value.TrimEnd('\"');
                                    currentTest.testMatlabScriptName = value;
                                }
                                if (parameter.Equals("usePowershellAnalysis")) currentTest.testPowershellAnalysis = bool.Parse(value);
                                if (parameter.Equals("powershellName"))
                                {
                                    value = value.TrimStart('\"');
                                    value = value.TrimEnd('\"');
                                    currentTest.testPowershellName = value;
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
