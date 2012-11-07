/*
 *     File : Window1.xaml.cs 
 * 
 *     Author : nived.sivadas@samraksh.com
 * 
 *     Description : contains the core control for the user interface of the test rig tool
 * 
 * 
 */



using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Collections.ObjectModel;
using System.Windows.Shapes;
using System.Xml;
using System.Threading;

namespace TestRig
{
    enum Tests
    {
        Timer,
        TimerNative,
        TimerCSharp,
        GPIO,
        GPIONative,
        GPIOCSharp,
        ADC,
        ADCNative,
        ADCCSharp,
        DAC,
        DACNative,
        DACCSharp,
        Radio,
        RadioNative,
        RadioCSharp,
        SDIO,
        SDIONative,
        SDIOCSharp,
        SPI,
        SPINative,
        SPICSharp,
        USB,
        USBNative,
        USBCSharp,
        NOR,
        NORNative,
        NORCSharp
    };
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        private OpenOCD vOpenOCD;

        private TestStatus tStatus;

        public string outTestStatus;

        public static System.Security.SecureString password;

        public static ObservableCollection<Test> _testCollection;

        public TestConfigParser configParser;

        public bool[] testsSelected;

        // Stores the working directory of the tool
        public static string ToolPath;

        // Stores the working directory of the test code
        public static string TestLocation;

        // Synchronizing the load and schedule test threads
        public static bool synchronize;

        

        public Window1()
        {
            InitializeComponent();

            ToolPath = tbTestToolPath.Text;
            TestLocation = tbTestSourcePath.Text;
            /* synchronize = false;

            password = new System.Security.SecureString();

            testsSelected = new bool[26];

            for (int i = 0; i < testsSelected.Length; i++)
                testsSelected[i] = false;

            
            
            vOpenOCD = OpenOCD.Instance;
            vOpenOCD.Intrfce = tbOCDInterface.Text;
            vOpenOCD.Target = tbOCDTarget.Text;
            vOpenOCD.Exe = tbOCDExe.Text;
            */
            configParser = new TestConfigParser(TestLocation);

            tStatus = TestStatus.Instance;
            tStatus.codeSourceryPath = tbBuildSourceryPath.Text;
            tStatus.mfInstallationPath = tbMFPath.Text;
            
            _testCollection = new ObservableCollection<Test>();
            DataContext = _testCollection;

            // Debug code /////////////
            Test tchsarp = new Test();
            tchsarp.testerName = nameOfTester.Text;
            tchsarp.testState = "UNAVAILABLE";
            tchsarp.testName = "GPIO";
            tchsarp.testLocation = "localRig";
            tchsarp.testPath = "";
            tchsarp.buildProj = "";
            tchsarp.testType = "C#";

            if ((tchsarp = configParser.verify(tchsarp)) != null)
            {
                tchsarp.testState = "QUEUED";
            }
            _testCollection.Add(tchsarp);
            tStatus.addTest(tchsarp);
            synchronize = true;
            ///////////////////////////
        }

        public static void showMessageBox(string message)
        {
            MessageBox.Show(message);
        }       

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            vOpenOCD.start();
        }

        private void Show_Click(object sender, RoutedEventArgs e)
        {
            
            //outBox.Text += "";
            //outBox.Text += vOpenOCD.ErrMessages;
        }

        private void Hide_Click(object sender, RoutedEventArgs e)
        {
            //outBox.Visibility = Visibility.Hidden;
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Upload_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < testsSelected.Length; i++)
            {
                Test t = new Test();
                t.testerName = nameOfTester.Text;
                t.testState = "UNAVAILABLE";

                // Need to think of a better way to do this, this is just ugly !!!
                // Leaving it this way because it is simple
                if (i == (int)Tests.Timer && (testsSelected[i] == true))
                {
                    t.testName = "Timer";
                    t.testLocation = RigSelect.Text;
                    t.testPath = "";
                    t.buildProj = "";
                    t.testType = "Native";

                    if ((t = configParser.verify(t)) != null)
                    {
                        t.testState = "QUEUED";
                    }
                    _testCollection.Add(t);
                    tStatus.addTest(t);

                    // Adding both tests when we select the main check box
                    Test tchsarp = new Test();
                    tchsarp.testerName = nameOfTester.Text;
                    tchsarp.testState = "UNAVAILABLE";
                    tchsarp.testName = "Timer";
                    tchsarp.testLocation = RigSelect.Text;
                    tchsarp.testPath = "";
                    tchsarp.buildProj = "";
                    tchsarp.testType = "C#";

                    if ((tchsarp = configParser.verify(tchsarp)) != null)
                    {
                        tchsarp.testState = "QUEUED";
                    }
                    _testCollection.Add(tchsarp);
                    tStatus.addTest(tchsarp);

                }
                else if (i == (int)Tests.TimerCSharp && (testsSelected[i] == true))
                {
                    t.testName = "Timer";
                    t.testLocation = RigSelect.Text;
                    t.testPath = "";
                    t.buildProj = "";
                    t.testType = "CSharp";
                    if ((t = configParser.verify(t)) != null)
                    {
                        t.testState = "QUEUED";
                    }
                    _testCollection.Add(t);
                    tStatus.addTest(t);
                }
                else if (i == (int)Tests.TimerNative && (testsSelected[i] == true))
                {
                    t.testName = "Timer";
                    t.testLocation = RigSelect.Text;
                    t.testPath = "";
                    t.buildProj = "";
                    t.testType = "Native";
                    if ((t = configParser.verify(t)) != null)
                    {
                        t.testState = "QUEUED";
                    }
                    _testCollection.Add(t);
                    tStatus.addTest(t);
                }
                else if (i == (int)Tests.GPIO && (testsSelected[i] == true))
                {
                    t.testName = "GPIO";
                    t.testLocation = RigSelect.Text;
                    t.testPath = "";
                    t.buildProj = "";
                    t.testType = "Native";
                    if ((t = configParser.verify(t)) != null)
                    {
                        t.testState = "QUEUED";
                    }
                    _testCollection.Add(t);
                    tStatus.addTest(t);

                    // Adding both tests when we select the main check box
                    Test tchsarp = new Test();
                    tchsarp.testerName = nameOfTester.Text;
                    tchsarp.testState = "UNAVAILABLE";
                    tchsarp.testName = "GPIO";
                    tchsarp.testLocation = RigSelect.Text;
                    tchsarp.testPath = "";
                    tchsarp.buildProj = "";
                    tchsarp.testType = "C#";
                    if ((tchsarp = configParser.verify(tchsarp)) != null)
                    {
                        tchsarp.testState = "QUEUED";
                    }
                    _testCollection.Add(tchsarp);
                    tStatus.addTest(tchsarp);
                }
                else if (i == (int)Tests.GPIOCSharp && (testsSelected[i] == true))
                {
                    t.testName = "Gpio";
                    t.testLocation = RigSelect.Text;
                    t.testPath = "";
                    t.buildProj = "";
                    t.testType = "CSharp";
                    if ((t = configParser.verify(t)) != null)
                    {
                        t.testState = "UNAVAILABLE";
                    }
                    _testCollection.Add(t);
                    tStatus.addTest(t);
                }
                else if (i == (int)Tests.GPIONative && (testsSelected[i] == true))
                {
                    t.testName = "Gpio";
                    t.testLocation = RigSelect.Text;
                    t.testPath = "";
                    t.buildProj = "";
                    t.testType = "Native";
                    if ((t = configParser.verify(t)) != null)
                    {
                        t.testState = "UNAVAILABLE";
                    }
                    _testCollection.Add(t);
                    tStatus.addTest(t);
                }
               
            }

            synchronize = true;
            //t.testerName = nameOfTester.Text;
            //t.testState = "QUEUED";
            //t.testName = testNameBox.Text;
            //t.testLocation = RigSelect.Text;
            //t.testPath = testPthBox.Text;
            //t.buildProj = buildProjBox.Text;         
        }



        public static void testStatusRefresh()
        {
           
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            //vLogic = new TestRig.LogicAnalyser();
            //vLogic.Start();
            //vLogic.ReadStart();
        }

        private void nameOfTester_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void passwordBox1_PasswordChanged(object sender, RoutedEventArgs e)
        {
            char[] tempPassword = passwordBox1.Password.ToCharArray();

            for (int i = 0; i < tempPassword.Length; i++)
                password.AppendChar(tempPassword[i]);
                       
        }

        private void stopLogic_Click(object sender, RoutedEventArgs e)
        {
            //vLogic.ReadStop();
        }

        private void checkBox3_Checked(object sender, RoutedEventArgs e)
        {
            testsSelected[(int)Tests.TimerCSharp] = true;
        }

        private void timerCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            testsSelected[(int) Tests.Timer] = true;
            
        }

        private void timerNativecheckBox_Checked(object sender, RoutedEventArgs e)
        {
            testsSelected[(int)Tests.TimerNative] = true;
        }

        private void gpioCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            testsSelected[(int)Tests.GPIO] = true;
        }

        private void gpioNativeCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            testsSelected[(int)Tests.GPIONative] = true;
        }

        private void gpioCSharpCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            testsSelected[(int)Tests.GPIOCSharp] = true;
        }

        private void btnDebug_click(object sender, RoutedEventArgs e)
        {
            LogicAnalyser logicTest;

            logicTest = new LogicAnalyser();
            Thread.Sleep(2000);
            logicTest.startMeasure("debugTest.out");
            Thread.Sleep(2000);
            logicTest.stopMeasure();
        }

    }
}
