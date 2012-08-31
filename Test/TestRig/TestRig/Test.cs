using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestRig
{
    public enum TestState
    {
        QUEUED = 0,
        BUILDING = 1,
        DEPLOYING = 2,
        TESTING = 3,
        TESTCOMPLETE = 4,
        
    }

    public class Test
    {

        static private int testId = 0;

        public int getId()
        {
            return testId;
        }

        public Test()
        {
            testId++;
        }

        public string testPath { get; set; }

        public string buildProj { get; set; }

        public string testName { get; set; }

        public string testerName { get; set; }

        public string testLocation { get; set; }

        public string testState { get; set; }

        public string progress { get; set; }

        public string directoryName { get; set; }

        public string buildEnvScriptName { get; set; }

        public string testScriptName { get; set; }
    }

    
}
