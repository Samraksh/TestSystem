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
        public string buildEnvScriptName { get; set; }
        public string testScriptName { get; set; }
        public string testMFVersionNum { get; set; }
        public string testGitOption { get; set; }
        public string testGitBranch { get; set; }
        public string testUsePrecompiledBinary { get; set; }

        // the following are set by the test parameter file
        public bool testUseLogic { get; set; }
        public int testSampleTimeMs { get; set; }
        public int testSampleFrequency { get; set; }
        public bool testUseExecutable { get; set; }
        public string testExecutableName { get; set; }
        public int testExecutableTimeoutMs { get; set; }
        public bool testMatlabAnalysis { get; set; }
        public string testMatlabScriptName { get; set; }
        public bool testPowershellAnalysis { get; set; }
        public string testPowershellName { get; set; }

        public TestDescription()
        {
            testReadComplete = false;
            testUseLogic = false;
            testSampleTimeMs = 500;
            testSampleFrequency = 4000000;
            testUseExecutable = false;
            testExecutableName = String.Empty;
            testExecutableTimeoutMs = 1000;
            testMatlabAnalysis = false;
            testMatlabScriptName = "analysis.m";
            testPowershellAnalysis = false;
            testPowershellName = "analysis.ps1";
            testUsePrecompiledBinary = String.Empty;
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
            this.buildEnvScriptName = copy.buildEnvScriptName;
            this.testScriptName = copy.testScriptName;
            this.testMFVersionNum = copy.testMFVersionNum;
            this.testGitOption = copy.testGitOption;
            this.testGitBranch = copy.testGitBranch;
            this.testUseLogic = copy.testUseLogic;
            this.testSampleTimeMs = copy.testSampleTimeMs;
            this.testSampleFrequency = copy.testSampleFrequency;
            this.testUseExecutable = copy.testUseExecutable;
            this.testExecutableName = copy.testExecutableName;
            this.testExecutableTimeoutMs = copy.testExecutableTimeoutMs;
            this.testMatlabAnalysis = copy.testMatlabAnalysis;
            this.testMatlabScriptName = copy.testMatlabScriptName;
            this.testPowershellAnalysis = copy.testPowershellAnalysis;
            this.testPowershellName = copy.testPowershellName;
            this.testUsePrecompiledBinary = copy.testUsePrecompiledBinary;
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
            returnString += "\t<TestUseLogic>\r\n\t" + testUseLogic.ToString() + "\r\n\t</TestUseLogic>\r\n";
            returnString += "\t<TestSampleTimeMs>\r\n\t" + testSampleTimeMs.ToString() + "\r\n\t</TestSampleTimeMs>\r\n";
            returnString += "\t<TestSampleFrequency>\r\n\t" + testSampleFrequency.ToString() + "\r\n\t</TestSampleFrequency>\r\n";
            returnString += "\t<TestUseExecutable>\r\n\t" + testUseExecutable.ToString() + "\r\n\t</TestUseExecutable>\r\n";
            returnString += "\t<TestExecutableName>\r\n\t" + testExecutableName + "\r\n\t</TestExecutableName>\r\n";
            returnString += "\t<TestExecutableTimeoutMs>\r\n\t" + testExecutableTimeoutMs.ToString() + "\r\n\t</TestExecutableTimeoutMs>\r\n";
            returnString += "\t<TestMatlabAnalysis>\r\n\t" + testMatlabAnalysis.ToString() + "\r\n\t</TestMatlabAnalysis>\r\n";
            returnString += "\t<TestMatlabScriptName>\r\n\t" + testMatlabScriptName + "\r\n\t</TestMatlabScriptName>\r\n";
            returnString += "\t<TestPowershellAnalysis>\r\n\t" + testPowershellAnalysis.ToString() + "\r\n\t</TestPowershellAnalysis>\r\n";
            returnString += "\t<TestPowershellName>\r\n\t" + testPowershellName + "\r\n\t</TestPowershellName>\r\n";
            returnString += "</Test>\r\n";

            return returnString;
        }
    }
}
