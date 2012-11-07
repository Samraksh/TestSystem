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
using System.Windows.Shapes;
using System.Net.Sockets;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Threading;
using System.IO;
using System.Xml;
using System.Net;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace TestRig
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    enum activateTestsOptions
    {
        parseTestFile,
        allTests
    };

    public partial class MainWindow : Window
    {
        private ReceiveSocket rxSocket;
        private TestLaunch testLaunch;
        public static Queue<TestDescription> testCollection;
        public static ObservableCollection<TestDescription> _displayTestCollection;
        static Mutex testCollectionMutex;
        public delegate void AddTestItem(TestDescription test);
        public delegate void RemoveTestItem();
        public delegate void DisplayUpdate();
        public AddTestItem addDelegate;
        public RemoveTestItem removeDelegate;
        public DisplayUpdate updateDelegate;
        public string textBuildSourceryPath;
        public string textMFPath;
        public string textTestSourcePath;
        public string textGitHubPath;
        public string textTestReceiptPath;
        public string textOCDInterface;
        public string textOCDTarget;
        public string textOCDExe;

        public MainWindow()
        {
            InitializeComponent();

            checkSettings();

            // to be used to protect testCollection from read/writes in various threads
            testCollectionMutex = new Mutex(false,"TestCollectionMutex");

            // the local machine test queue to be run (if this is the test machine)
            testCollection = new Queue<TestDescription>();

            // used to display the current test queue in the Test Status tab
            _displayTestCollection = new ObservableCollection<TestDescription>();
            DataContext = _displayTestCollection;

            // these delegates are used by other threads to update _displayTestCollection
            addDelegate = new AddTestItem(AddTestItemMethod);
            removeDelegate = new RemoveTestItem(RemoveTestItemMethod);
            updateDelegate = new DisplayUpdate(DisplayUpdateMethod);
            
            // sometimes openOCD is running so we will check and warn here.
            bool openOCDRunning = false;
            foreach (Process clsProcess in Process.GetProcesses())
            {
                if (clsProcess.ProcessName.Contains("openoc"))
                    openOCDRunning = true;
            }

            if (openOCDRunning == false)
                Initialize();
            else
            {
                MessageBox.Show("OpenOCD is already running.\r\nStop the OpenOCD process and try again.");
                Environment.Exit(0);
            }

        }

        public void Initialize()
        {
            try
            {                                
                // RxSocket is used to send selected test queue to the tester and to listen for incoming test queues if this machine is the tester
                rxSocket = new ReceiveSocket(testCollection, testCollectionMutex, this);

                // testLaunch is used to execute any tests in the local test queue if this machine is the tester
                testLaunch = new TestLaunch(testCollection, testCollectionMutex, this);

                // read Test XML file to discover which tests are available to test
                activateTests(activateTestsOptions.parseTestFile);                
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("connectToEmote: " + ex.ToString());
            }
        }

        public void Kill()
        {
            try
            {
                // killing any active threads before program quits
                System.Diagnostics.Debug.WriteLine("Killing active threads...");
                
                if (rxSocket != null)
                {
                    rxSocket.KillListenThread();
                }
                if (testLaunch != null)
                {
                    testLaunch.KillLaunchThread();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("killConnectionToEmote: " + ex.ToString());
            }
        }

        public void OnClosing(object sender, CancelEventArgs e)
        {
            // saving tester paths
            saveSettings();
            Properties.Settings.Default.Save();

            // killing all running threads
            Kill();
            
            e.Cancel = false;
        }

        public void AddTestItemMethod(TestDescription test)
        {
            // the method used by other threads to add to _displayTestCollection
            _displayTestCollection.Add(test);
        }

        public void RemoveTestItemMethod()
        {
            // the method used by other threads to remove items from  _displayTestCollection
            _displayTestCollection.RemoveAt(0);                    
        }

        public void DisplayUpdateMethod()
        {
            listViewStatus.Items.Refresh();
        }

        private void tbTestSourcePath_TextChanged(object sender, TextChangedEventArgs e)
        {
            // if test source path changed then we will automatically enable/diable test options
            activateTests(activateTestsOptions.parseTestFile);

            // this string is used because other threads are not allowed to access the text box directly
            textTestSourcePath = tbTestSourcePath.Text;
        }

        private void activateTests(activateTestsOptions option)
        {
            bool initValue;
            if (option == activateTestsOptions.parseTestFile)
                initValue = false;
            else
                initValue = true;

            // TODO: make checkbox initialization into cleaner loop
            timerCSharpcheckBox.IsEnabled = initValue;
            timerCSharpcheckBox.IsChecked = false;

            timerNativecheckBox.IsEnabled = initValue;
            timerNativecheckBox.IsChecked = false;

            gpioCSharpCheckBox.IsEnabled = initValue;
            gpioCSharpCheckBox.IsChecked = false;

            gpioNativeCheckBox.IsEnabled = initValue;
            gpioNativeCheckBox.IsChecked = false;

            if (option == activateTestsOptions.parseTestFile)
            {                
                try
                {
                    string xmlFileName = tbTestSourcePath.Text + "\\tests.xml";
                    StreamReader testReader = new StreamReader(xmlFileName);
                    // Create an XmlReader
                    using (XmlReader reader = XmlReader.Create(testReader))
                    {

                        System.Diagnostics.Debug.WriteLine("Starting to available test XML test file.");
                        while (reader != null)
                        {
                            // read in each defined test within the configuration file and activate the appropriate checkbox
                            TestDescription readTest = rxSocket.readXMLTest(reader);

                            if (readTest.testName == "Timer" && readTest.testType == "C#")
                            {
                                timerCSharpcheckBox.IsEnabled = true;
                            }
                            else if (readTest.testName == "Timer" && readTest.testType == "Native")
                            {
                                timerNativecheckBox.IsEnabled = true;
                            }
                            else if (readTest.testName == "GPIO" && readTest.testType == "C#")
                            {
                                gpioCSharpCheckBox.IsEnabled = true;
                            }
                            else if (readTest.testName == "GPIO" && readTest.testType == "Native")
                            {
                                gpioNativeCheckBox.IsEnabled = true;
                            }
                        }

                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Activate XML reading ended with exception: " + ex.Message);
                }
            }
        }

        private void Upload_Click(object sender, RoutedEventArgs e)
        {
            // once upload button is clicked then we will write all desired tests to a XML file and send to the test machine (sent to localhost if this machine is the test machine)
            TestDescription testTimerCS = null;
            TestDescription testTimerNative = null;
            TestDescription testGPIOCS = null;
            TestDescription testGPIONative = null;

            string xmlFileName = tbTestSourcePath.Text + "\\tests.xml";
            StreamReader testReader = new StreamReader(xmlFileName);
            // Create an XmlReader
            using (XmlReader reader = XmlReader.Create(testReader))
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("Reading in all available tests.");
                    while (reader != null)
                    {
                        TestDescription readTest = rxSocket.readXMLTest(reader);

                        // if the follow items are specified already we keep those parameters, otherwise those test parameters are populated here
                        if (readTest.testerName == "")
                            readTest.testerName = System.Environment.UserName.ToString();
                        if (readTest.testLocation == "")
                            readTest.testLocation = System.Environment.MachineName.ToString();
                        if (readTest.testMFVersionNum == "")
                            readTest.testMFVersionNum = "4.0";
                        if (readTest.testGitOption == "")
                            readTest.testGitOption = cbCodeLocation.Text;
                        if (readTest.testGitBranch == "")
                            readTest.testGitBranch = tbCodeBranch.Text;

                        if (readTest.testName == "Timer" && readTest.testType == "C#")
                        {
                            testTimerCS = new TestDescription(readTest);
                        }
                        else if (readTest.testName == "Timer" && readTest.testType == "Native")
                        {
                            testTimerNative = new TestDescription(readTest);
                        }
                        else if (readTest.testName == "GPIO" && readTest.testType == "C#")
                        {
                            testGPIOCS = new TestDescription(readTest);
                        }
                        else if (readTest.testName == "GPIO" && readTest.testType == "Native")
                        {
                            testGPIONative = new TestDescription(readTest);
                        }                        
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Upload XML reading ended with exception: " + ex.Message);
                }
            }           

            // upon click any tests selected will generate a test description  in a file which will then be sent to the test machine (to localhost if this machine is the tester)
            string fileName = "test.config";            
            using (StreamWriter writer = new StreamWriter(fileName))
            {
                writer.WriteLine("<TestSuite>");
                if (timerNativecheckBox.IsChecked == true)
                {
                    if (testTimerNative == null)
                        MessageBox.Show("Test Timer Native test not defined.");
                    else
                        writer.Write(testTimerNative.ToString());
                }
                if (timerCSharpcheckBox.IsChecked == true)
                {
                    if (testTimerCS == null)
                        MessageBox.Show("Test Timer C# test not defined.");
                    else
                        writer.Write(testTimerCS.ToString());
                }
                if (gpioNativeCheckBox.IsChecked == true)
                {
                    if (testGPIONative == null)
                        MessageBox.Show("Test GPIO Native test not defined.");
                    else
                        writer.Write(testGPIONative.ToString());
                }
                if (gpioCSharpCheckBox.IsChecked == true)
                {
                    if (testGPIOCS == null)
                        MessageBox.Show("Test GPIO C# test not defined.");
                    else
                        writer.Write(testGPIOCS.ToString());
                }
                writer.WriteLine("</TestSuite>");
            }
            // sending the generated test file to the test machine queue
            rxSocket.uploadTests();
        }

        private void tbBuildSourceryPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            textBuildSourceryPath = tbBuildSourceryPath.Text;
        }

        private void tbMFPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            textMFPath = tbMFPath.Text;
        }

        private void tbGitHubPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            textGitHubPath = tbGitHubPath.Text;
        }

        private void tbTestReceiptPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            textTestReceiptPath = tbTestReceiptPath.Text;
        }

        private void tbOCDInterface_TextChanged(object sender, TextChangedEventArgs e)
        {
            textOCDInterface = tbOCDInterface.Text;
        }

        private void tbOCDTarget_TextChanged(object sender, TextChangedEventArgs e)
        {
            textOCDTarget = tbOCDTarget.Text;
        }

        private void tbOCDExe_TextChanged(object sender, TextChangedEventArgs e)
        {
            textOCDExe = tbOCDExe.Text;
        }

        public static void showMessageBox(string message)
        {
            MessageBox.Show(message);
        }

        private void buttonDebug_Click(object sender, RoutedEventArgs e)
        {

        }

        private void checkSettings()
        {
            try
            {
                // checking to make sure paths actually exist
                string tempPath;
                tbOCDExe.Text = Properties.Settings.Default.OCDExe.ToString();
                if (File.Exists(tbOCDExe.Text) == false)
                {
                    MessageBox.Show("OpenOCD executable: " + tbOCDExe.Text + " does not exist.");
                }

                tbOCDInterface.Text = Properties.Settings.Default.OCDInterface.ToString();
                tempPath = (Directory.GetParent(tbOCDExe.Text) + "\\" + tbOCDInterface.Text).ToString();
                if (File.Exists(tempPath) == false)
                {
                    MessageBox.Show("OpenOCD Interface file: " + tempPath + " does not exist.");
                }

                tbOCDTarget.Text = Properties.Settings.Default.OCDTarget.ToString();
                tempPath = (Directory.GetParent(tbOCDExe.Text) + "\\" + tbOCDTarget.Text).ToString();
                if (File.Exists(tempPath) == false)
                {
                    MessageBox.Show("OpenOCD Target file: " + tempPath + " does not exist.");
                }

                tbBuildSourceryPath.Text = Properties.Settings.Default.CSPath.ToString();
                if (Directory.Exists(tbBuildSourceryPath.Text) == false)
                {
                    MessageBox.Show("Codesourcery path: " + tbBuildSourceryPath.Text + " does not exist.");
                }

                tbMFPath.Text = Properties.Settings.Default.MFPath.ToString();
                if (Directory.Exists(tbMFPath.Text) == false)
                {
                    MessageBox.Show("Microframework path: " + tbMFPath.Text + " does not exist.");
                }

                tbTestToolPath.Text = Properties.Settings.Default.TTPath.ToString();
                if (Directory.Exists(tbTestToolPath.Text) == false)
                {
                    MessageBox.Show("Test tool path: " + tbTestToolPath.Text + " does not exist.");
                }

                tbTestSourcePath.Text = Properties.Settings.Default.TSPath.ToString();
                if (Directory.Exists(tbTestSourcePath.Text) == false)
                {
                    MessageBox.Show("Test source path: " + tbTestSourcePath.Text + " does not exist.");
                }

                tbGitHubPath.Text = Properties.Settings.Default.GHPath.ToString();
                if (Directory.Exists(tbGitHubPath.Text) == false)
                {
                    MessageBox.Show("Git Hub path: " + tbGitHubPath.Text + " does not exist.");
                }

                tbTestReceiptPath.Text = Properties.Settings.Default.TRPath.ToString();
                if (Directory.Exists(tbTestReceiptPath.Text) == false)
                {
                    MessageBox.Show("Test Receipt path: " + tbTestReceiptPath.Text + " does not exist.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("checkRegistry: " + ex.ToString());
            }
        }

        private void saveSettings()
        {
            try
            {
                // paths are saved to a settings file
                Properties.Settings.Default["OCDInterface"] = tbOCDInterface.Text;
                Properties.Settings.Default["OCDTarget"] = tbOCDTarget.Text;
                Properties.Settings.Default["OCDExe"] = tbOCDExe.Text;
                Properties.Settings.Default["CSPath"] = tbBuildSourceryPath.Text;
                Properties.Settings.Default["MFPath"] = tbMFPath.Text;
                Properties.Settings.Default["TTPath"] = tbTestToolPath.Text;
                Properties.Settings.Default["TSPath"] = tbTestSourcePath.Text;
                Properties.Settings.Default["GHPath"] = tbGitHubPath.Text;
                Properties.Settings.Default["TRPath"] = tbTestReceiptPath.Text;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("saveToRegistry: " + ex.ToString());
            }
        }

        private void cbCodeLocation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {            
            if (tbCodeBranch == null)
            {
                // buttons have not yet been defined by InitializeComponent
                return;
            }
            string textCbCodeLocation = cbCodeLocation.SelectedItem.ToString();
            if (textCbCodeLocation.Contains("Use local code"))
            {
                // read Test XML file to discover which tests are available to test
                activateTests(activateTestsOptions.parseTestFile);

                lblBranch.Visibility = Visibility.Hidden;
                tbCodeBranch.Visibility = Visibility.Hidden;
            }
            else if (textCbCodeLocation.Contains("Use archive code"))
            {
                activateTests(activateTestsOptions.allTests);
                lblBranch.Visibility = Visibility.Hidden;
                tbCodeBranch.Visibility = Visibility.Hidden;
            }
            else if (textCbCodeLocation.Contains("Use archive branch code"))
            {
                activateTests(activateTestsOptions.allTests);
                lblBranch.Visibility = Visibility.Visible;
                tbCodeBranch.Visibility = Visibility.Visible;
            }             
        }        
    }
}
