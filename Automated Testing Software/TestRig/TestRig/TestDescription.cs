using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestRig
{
    public class TestDescription
    {
        public bool testReadComplete;
        public string testName { get; set; }
        public string testType { get; set; }
        public string testDescription { get; set; }
        public string testPath { get; set; }
        public string buildProj { get; set; }
        public string testerName { get; set; }
        public string testLocation { get; set; }
        public string testState { get; set; }
        public string testProgress { get; set; }
        public string directoryName { get; set; }
        public string testMFVersionNum { get; set; }
        public string testGitOption { get; set; }
        public string testGitBranch { get; set; }
        public string testUsePrecompiledBinary { get; set; }
        public string testHardware { get; set; }
        public string testSolution { get; set; }
        public string testMemoryType { get; set; }
        public string testSolutionType { get; set; }
        public string testGCCVersion { get; set; }
        public string testSupporting { get; set; }
        public string testJTAGHarness { get; set; }

        // the following are set by the test parameter file
        public int testTimeout { get; set; }
        public string testUseLogic { get; set; }
        public int testSampleTimeMs { get; set; }
        public int testSampleFrequency { get; set; }
        public bool testUseCOM { get; set; }
        public string testForceCOM { get; set; }
        public string testCOMParameters { get; set; }
        public bool testUseScript { get; set; }
        public string testScriptName { get; set; }
        public int testScriptTimeoutMs { get; set; }
        public string testAnalysis { get; set; }
        public string testAnalysisScriptName { get; set; }
        public bool testUseResultsFile { get; set; }
        public string testResultsFileName { get; set; }    

        public TestDescription()
        {
            testReadComplete = false;
            testTimeout = 60000;
            testUseLogic = "false";
            testSampleTimeMs = 500;
            testSampleFrequency = 4000000;
            testUseScript = false;
            testScriptName = String.Empty;
            testScriptTimeoutMs = 1000;
            testAnalysis = "none";
            testAnalysisScriptName = "analyze.m";
            testUseResultsFile = false;
            testResultsFileName = "results.txt";
            testUsePrecompiledBinary = String.Empty;
            testHardware = String.Empty;
            testSolution = String.Empty;
            testMemoryType = String.Empty;
            testSolutionType = String.Empty;
            testGCCVersion = String.Empty;
            testSupporting = String.Empty;
            testJTAGHarness = String.Empty;
            testUseCOM = false;
            testForceCOM = String.Empty;
            testCOMParameters = String.Empty;
        }

        public TestDescription(TestDescription copy)
        {
            this.testName = copy.testName;
            this.testType = copy.testType;
            this.testDescription = copy.testDescription;
            this.testPath = copy.testPath;
            this.buildProj = copy.buildProj;
            this.testerName = copy.testerName;
            this.testLocation = copy.testLocation;
            this.testState = copy.testState;
            this.testProgress = copy.testProgress;
            this.directoryName = copy.directoryName;
            this.testMFVersionNum = copy.testMFVersionNum;
            this.testGitOption = copy.testGitOption;
            this.testGitBranch = copy.testGitBranch;
            this.testTimeout = copy.testTimeout;
            this.testUseLogic = copy.testUseLogic;
            this.testSampleTimeMs = copy.testSampleTimeMs;
            this.testSampleFrequency = copy.testSampleFrequency;
            this.testUseScript = copy.testUseScript;
            this.testScriptName = copy.testScriptName;
            this.testScriptTimeoutMs = copy.testScriptTimeoutMs;
            this.testAnalysis = copy.testAnalysis;
            this.testAnalysisScriptName = copy.testAnalysisScriptName;
            this.testUseResultsFile = copy.testUseResultsFile;
            this.testResultsFileName = copy.testResultsFileName;
            this.testUsePrecompiledBinary = copy.testUsePrecompiledBinary;
            this.testHardware = copy.testHardware;
            this.testSolution = copy.testSolution;
            this.testMemoryType = copy.testMemoryType;
            this.testSolutionType = copy.testSolutionType;
            this.testGCCVersion = copy.testGCCVersion;
            this.testSupporting = copy.testSupporting;
            this.testJTAGHarness = copy.testJTAGHarness;
            this.testUseCOM = copy.testUseCOM;
            this.testForceCOM = copy.testForceCOM;
            this.testCOMParameters = copy.testCOMParameters;
        }

        public override string ToString()
        {
            string returnString = String.Empty;

            returnString += "<Test Name='" + testName + "' Type = '" + testType + "'>\r\n";
            returnString += "\t<Description>\r\n\t" + testDescription + "\r\n\t</Description>\r\n";
            returnString += "\t<TestPath>\r\n\t" + testPath + "\r\n\t</TestPath>\r\n";
            returnString += "\t<TestProjName>\r\n\t" + buildProj + "\r\n\t</TestProjName>\r\n";
            returnString += "\t<TesterName>\r\n\t" + testerName + "\r\n\t</TesterName>\r\n";
            returnString += "\t<TestLocation>\r\n\t" + testLocation + "\r\n\t</TestLocation>\r\n";
            returnString += "\t<TestMFVersionNum>\r\n\t" + testMFVersionNum + "\r\n\t</TestMFVersionNum>\r\n";
            returnString += "\t<TestGitOption>\r\n\t" + testGitOption + "\r\n\t</TestGitOption>\r\n";
            returnString += "\t<TestGitBranch>\r\n\t" + testGitBranch + "\r\n\t</TestGitBranch>\r\n";
            returnString += "\t<TestUsePrecompiledBinary>\r\n\t" + testUsePrecompiledBinary + "\r\n\t</TestUsePrecompiledBinary>\r\n";
            returnString += "\t<TestHardware>\r\n\t" + testHardware + "\r\n\t</TestHardware>\r\n";
            returnString += "\t<TestSolution>\r\n\t" + testSolution + "\r\n\t</TestSolution>\r\n";
            returnString += "\t<TestMemoryType>\r\n\t" + testMemoryType + "\r\n\t</TestMemoryType>\r\n";
            returnString += "\t<TestSolutionType>\r\n\t" + testSolutionType + "\r\n\t</TestSolutionType>\r\n";
            returnString += "\t<TestGCCVersion>\r\n\t" + testGCCVersion + "\r\n\t</TestGCCVersion>\r\n";
            returnString += "\t<TestSupporting>\r\n\t" + testSupporting + "\r\n\t</TestSupporting>\r\n";
            returnString += "\t<TestJTAGHarness>\r\n\t" + testJTAGHarness + "\r\n\t</TestJTAGHarness>\r\n";
            returnString += "\t<TestTimeout>\r\n\t" + testTimeout.ToString() + "\r\n\t</TestTimeout>\r\n";
            returnString += "\t<TestUseLogic>\r\n\t" + testUseLogic + "\r\n\t</TestUseLogic>\r\n";
            returnString += "\t<TestSampleTimeMs>\r\n\t" + testSampleTimeMs.ToString() + "\r\n\t</TestSampleTimeMs>\r\n";
            returnString += "\t<TestSampleFrequency>\r\n\t" + testSampleFrequency.ToString() + "\r\n\t</TestSampleFrequency>\r\n";
            returnString += "\t<testUseScript>\r\n\t" + testUseScript.ToString() + "\r\n\t</testUseScript>\r\n";
            returnString += "\t<testScriptName>\r\n\t" + testScriptName + "\r\n\t</testScriptName>\r\n";
            returnString += "\t<testScriptTimeoutMs>\r\n\t" + testScriptTimeoutMs.ToString() + "\r\n\t</testScriptTimeoutMs>\r\n";
            returnString += "\t<TestAnalysis>\r\n\t" + testAnalysis.ToString() + "\r\n\t</TestAnalysis>\r\n";
            returnString += "\t<TestAnalysisScriptName>\r\n\t" + testAnalysisScriptName + "\r\n\t</TestAnalysisScriptName>\r\n";
            returnString += "\t<TestUseResultsFile>\r\n\t" + testUseResultsFile.ToString() + "\r\n\t</TestUseResultsFile>\r\n";
            returnString += "\t<TestResultsFileName>\r\n\t" + testResultsFileName + "\r\n\t</TestResultsFileName>\r\n";
            returnString += "\t<TestUseCOM>\r\n\t" + testUseCOM.ToString() + "\r\n\t</TestUseCOM>\r\n";
            returnString += "\t<TestForceCOM>\r\n\t" + testForceCOM + "\r\n\t</TestForceCOM>\r\n";
            returnString += "\t<TestCOMParameters>\r\n\t" + testCOMParameters + "\r\n\t</TestCOMParameters>\r\n";
            returnString += "</Test>\r\n";

            return returnString;
        }
    }
}
