using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TestRig
{
    public class TestReceipt
    {
        public bool testComplete;
        public string testResult;
        public bool testPass;
        public TimeSpan testDuration;
        public double testAccuracy;
        public DateTime testDateTime;
        public string testReturnParameter1;
        public string testReturnParameter2;
        public string testReturnParameter3;
        public string testReturnParameter4;
        public string testReturnParameter5;
        public string testExecutionMachine;
        public TestDescription testDescription;

        public TestReceipt(TestDescription currentTest)
        {
            testComplete = false;
            testResult = "No test executed";
            testPass = false;
            testDuration = TimeSpan.Zero;
            testAccuracy = 0;
            testDateTime = DateTime.Now;
            testReturnParameter1 = String.Empty;
            testReturnParameter2 = String.Empty;
            testReturnParameter3 = String.Empty;
            testReturnParameter4 = String.Empty;
            testReturnParameter5 = String.Empty;
            testExecutionMachine = String.Empty;
            testDescription = currentTest;
        }

        public TestReceipt(TestReceipt copy)
        {
            this.testComplete = copy.testComplete;
            this.testResult = copy.testResult;
            this.testPass = copy.testPass;
            this.testDuration = copy.testDuration;
            this.testAccuracy = copy.testAccuracy;
            this.testDateTime = copy.testDateTime;
            this.testReturnParameter1 = copy.testReturnParameter1;
            this.testReturnParameter2 = copy.testReturnParameter2;
            this.testReturnParameter3 = copy.testReturnParameter3;
            this.testReturnParameter4 = copy.testReturnParameter4;
            this.testReturnParameter5 = copy.testReturnParameter5;
            this.testExecutionMachine = copy.testExecutionMachine;
            this.testDescription = copy.testDescription;
        }

        public override string ToString()
        {
            string returnString = String.Empty;

            returnString += "<TestResults>\r\n";
            returnString += "\t<TestComplete>\r\n\t" + testComplete.ToString() + "\r\n\t</TestComplete>\r\n";
            returnString += "\t<TestResult>\r\n\t" + testResult + "\r\n\t</TestResult>\r\n";
            returnString += "\t<TestPass>\r\n\t" + testPass.ToString() + "\r\n\t</TestPass>\r\n";
            returnString += "\t<TestDateTime>\r\n\t" + String.Format("{0:yyyy'-'MM'-'dd HH':'mm':'ss}", testDateTime) + "\r\n\t</TestDateTime>\r\n";
            returnString += "\t<TestDuration>\r\n\t" + testDuration.ToString() + "\r\n\t</TestDuration>\r\n";
            returnString += "\t<TestAccuracy>\r\n\t" + testAccuracy.ToString() + "\r\n\t</TestAccuracy>\r\n";
            returnString += "\t<TestReturnParameter1>\r\n\t" + testReturnParameter1 + "\r\n\t</TestReturnParameter1>\r\n";
            returnString += "\t<TestReturnParameter2>\r\n\t" + testReturnParameter2 + "\r\n\t</TestReturnParameter2>\r\n";
            returnString += "\t<TestReturnParameter3>\r\n\t" + testReturnParameter3 + "\r\n\t</TestReturnParameter3>\r\n";
            returnString += "\t<TestReturnParameter4>\r\n\t" + testReturnParameter4 + "\r\n\t</TestReturnParameter4>\r\n";
            returnString += "\t<TestReturnParameter5>\r\n\t" + testReturnParameter5 + "\r\n\t</TestReturnParameter5>\r\n";
            returnString += "\t<TestExecutionMachine>\r\n\t" + testExecutionMachine + "\r\n\t</TestExecutionMachine>\r\n";            
            returnString += "\t<TestDescription>\r\n\t" + testDescription.ToString() + "\r\n\t</TestDescription>\r\n";
            returnString += "</TestResults>\r\n";

            return returnString;
        }

        public string WriteFile(string path)
        {            
            DateTime fileTime = DateTime.Now;
            string fileName;

            fileName = fileTime.Year.ToString() + fileTime.Month.ToString("D2") + fileTime.Day.ToString("D2") + "_" + fileTime.Hour.ToString("D2") + fileTime.Minute.ToString("D2") + "_";            
            //stripping first two digits of year
            fileName = fileName.Substring(2);

            // removing .csproj or .proj from project name
            int index = testDescription.buildProj.LastIndexOf('.');
            string strippedProject = testDescription.buildProj.Substring(0, index);
            string fullFileName;

            if (testPass == true)
                fullFileName = fileName + strippedProject +"_receipt.xml";
            else
                fullFileName = fileName + strippedProject + "_FAIL_receipt.xml";
            // if a file already exists a number is appended to the file so as to not overwrite the file
            int num = 1;
            while (File.Exists(path + @"\" + fullFileName))
            {
                fullFileName = fileName + strippedProject + "_receipt" + num.ToString() + ".xml";
                num++;
            }

            using (StreamWriter writer = new StreamWriter(path + @"\" + fullFileName))
            {
                writer.WriteLine(this.ToString());
            }

            return path + @"\" + fullFileName;
        }
    }
}
