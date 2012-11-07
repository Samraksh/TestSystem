using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace TestRig
{
    public class TestConfigParser
    {
        XmlTextReader reader;

        string configFile;

        Test[] tests;


        // Pass the path of the test configuration file as input
        public TestConfigParser(string path)
        {
            try
            {
                path = path.Replace("\n", String.Empty);
                path = path.Replace("\t", String.Empty);
                path = path.Replace("\r", String.Empty);
                configFile = path + @"\TestSys\tests.xml";

                reader = new XmlTextReader(configFile);

                tests = new Test[10];

                for (int i = 0; i < tests.Length; i++)
                    tests[i] = new Test();

                loadTests();
            }
            catch (Exception e)
            {
                Window1.showMessageBox(e.Message);
            }

        }

        public Test verify(Test t)
        {
            for (int i = 0; i < tests.Length; i++)
            {
                if (tests[i].testName == t.testName && tests[i].testType == t.testType)
                {
                    t.testPath = tests[i].testPath;
                    t.buildProj = tests[i].buildProj;
                    return t;
                }
            }
            return null;
        }

        public void loadTests()
        {
            int testCounter = -1;
            bool ispath = false;
            bool isProjName = false;
            try
            {

                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (reader.Name == "Test")
                            {
                                testCounter++;
                                while (reader.MoveToNextAttribute())
                                {
                                    if (reader.Name == "Name")
                                        tests[testCounter].testName = reader.Value;
                                    else if (reader.Name == "Type")
                                        tests[testCounter].testType = reader.Value;
                                }
                            }
                            else if (reader.Name == "TestPath")
                            {
                                ispath = true;
                            }
                            else if (reader.Name == "TestProjName")
                            {
                                isProjName = true;
                            }
                            break;
                        case XmlNodeType.Text:
                            if (ispath == true)
                            {
                                tests[testCounter].testPath = reader.Value;
                                ispath = false;
                            }
                            else if (isProjName == true)
                            {
                                tests[testCounter].buildProj = reader.Value;
                                isProjName = false;
                            }
                            else
                            {
                                tests[testCounter].testDescription = reader.Value;
                            }
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Window1.showMessageBox(e.Message);
            }
        }
    }
}
