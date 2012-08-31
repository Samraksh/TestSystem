﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Threading;

namespace TestRig
{
    public class TestStatus
    {
        Queue<Test> _testCollection;

        Thread m_testScheduler;

        msbuild build;

        public string codeSourceryPath { get; set; }

        public string mfInstallationPath {get; set;}

        GDB gdb;
        
        private static TestStatus instance;

        private TestStatus()
        {
            //build = msbuild.Instance;
            //gdb = GDB.Instance;
            _testCollection = new Queue<Test>();
            m_testScheduler = new Thread(new ThreadStart(scheduleTest));
            m_testScheduler.Start();
        }

        public static TestStatus Instance
        {
            get
            {
                if (instance == null)
                    instance = new TestStatus();

                return instance;
            }
            
        }

        public void scheduleTest()
        {
            while (true)
            {
                // if there are no tests in the system, busy wait
                while (_testCollection.Count == 0) ;

                Test t = _testCollection.Dequeue();

                PowerShell p = new PowerShell(codeSourceryPath,mfInstallationPath);

                p.setTestEnv(t);

                
                if (t.testLocation == "localRig")
                    p.ConnectLocal();
                else if (t.testLocation == "DublinRig")
                    p.ConnectRemote();

                p.createPowerShellRunspace();

                //p.ExecuteEnvScript();
               

                p.generateTestScript();

                /*
                if (t.testLocation == "localRig")
                    p.ConnectLocal();
                else if (t.testLocation == "DublinRig")
                    p.ConnectRemote();
                */
                
                /*
                t.testState = "BUILDING";

                DisplaySynch.synchronize(t);

                build.Init(t);
                build.Start();
                build.SetEnv();
                build.Clean();
                build.Build();
                //build.Kill();

                t.testState = "BUILDCOMPLETE";

                DisplaySynch.synchronize(t);

                gdb.Init(t);
                gdb.Start();
                gdb.Load();

                t.testState = "LOADCOMPLETE";

                DisplaySynch.synchronize(t);

                Thread.Sleep(2000);
                 */
            }


            
        }

        public Queue<Test> TestCollection
        {
            get { return _testCollection; }
        }

        public void addTest(Test t)
        {
            _testCollection.Enqueue(t);
        }
        
        

    }

    public static class DisplaySynch
    {
        public static void synchronize(Test t)
        {
            for (int i = 0; i < Window1._testCollection.Count; i++)
            {
                if (Window1._testCollection[i].getId() == t.getId())
                {
                    Window1._testCollection[i].testState = t.testState;
                    
                }
            }
        }
    }
}
