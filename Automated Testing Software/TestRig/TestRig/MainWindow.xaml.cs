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
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Net;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using System.Globalization;


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
        private static ReceiveSocket rxSocket;
        private TestLaunch testLaunch;
        public static Queue<TestDescription> testCollection;
        public static ObservableCollection<TestDescription> _displayTestCollection;
        static Mutex testCollectionMutex;
        public delegate void AddTestItem(TestDescription test);
        public delegate void RemoveTestItem();
        public delegate void ClearTestItems();
        public delegate void DisplayUpdate();
        public delegate void StartTestTimer();
        public delegate void StopTestTimer();
        public delegate void PrintError(string message);
        public delegate void AddTestResult(TestReceipt testReceipt);
        public AddTestItem addDelegate;
        public RemoveTestItem removeDelegate;
        public ClearTestItems clearDelegate;
        public DisplayUpdate updateDelegate;
        public PrintError printErrorDelegate;
        public StartTestTimer startTestTimerDelegate;
        public StopTestTimer stopTestTimerDelegate;
        public AddTestResult addTestResultDelegate;
        public string textBuildSourceryPath;
        public string textMFPath_4_0;
        public string textMFPath_4_3;
        public string textTestSourcePath;
        public string textTestReceiptPath;
        public string textOCDInterfacePrimary;
        public string textOCDInterfaceSecondary1;
        public string textOCDTarget;
        public string textOCDExe;
        public string textGitPath;
        public static string textMFPathSelection;
        public static string textGitCodeLocation;
        public static string textGitCodeBranch;
        public static string textHardware;
        public static string textSolution;
        public static string textMemoryType;
        public static string textSolutionType;
        public string textGCCVersion;
        public static string textMFSelected;
        public string textCodeBuildSelected;
        public static string textJTAGHarness;
        public static string textPowerAutomateSelected;
        public string textCOMPortPrimary;
        public string textCOMPortSecondary1;
        public int COMPortSelectionPrimary;
        public int COMPortSelectionSecondary1;
        public static TestDescription[] availableTests;
        public static TestResults[] availableTestResults;
        private static Tasks _tasks;
        private static TestResultss _results;
        private static int testNum;
        private static int resultNum;
        private bool settingsInitialized = false;
        private int batchCall;
        private bool commandLineMode = false;
        private DispatcherTimer testTimer;
        private DateTime dateTestTimerBegin;

        public MainWindow()
        {
#if DEBUG
            System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level = System.Diagnostics.SourceLevels.Critical;
#endif
            InitializeComponent();

            readSettings();
            checkPaths();

            // to be used to protect testCollection from read/writes in various threads
            testCollectionMutex = new Mutex(false, "TestCollectionMutex");

            // the local machine test queue to be run (if this is the test machine)
            testCollection = new Queue<TestDescription>();

            // used to display the current test queue in the Test Status tab
            _displayTestCollection = new ObservableCollection<TestDescription>();
            DataContext = _displayTestCollection;

            // these delegates are used by other threads to update _displayTestCollection
            addDelegate = new AddTestItem(AddTestItemMethod);
            removeDelegate = new RemoveTestItem(RemoveTestItemMethod);
            clearDelegate = new ClearTestItems(ClearTestItemsMethod);
            updateDelegate = new DisplayUpdate(DisplayUpdateMethod);
            startTestTimerDelegate = new StartTestTimer(StartTestTimerMethod);
            stopTestTimerDelegate = new StopTestTimer(StopTestTimerMethod);
            printErrorDelegate = new PrintError(PrintErrorMethod);
            addTestResultDelegate = new AddTestResult(AddResult);

            // sometimes openOCD is running so we will check and warn here.
            bool openOCDRunning = false;
            bool logicRunning = false;
            foreach (Process clsProcess in Process.GetProcesses())
            {
                if (clsProcess.ProcessName.Contains("openoc"))
                    openOCDRunning = true;
                if (clsProcess.ProcessName.Contains("Logic"))
                    logicRunning = true;
            }

            if (openOCDRunning == true){
                MessageBox.Show("OpenOCD is already running.\r\nThe test system will not be able to program.");
                //Environment.Exit(0);
            }
            else if (logicRunning == true)
            {
                MessageBox.Show("Logic analyzer is already running.\r\nTests will fail unless analyzer program is closed.");
                //Environment.Exit(0);
            }
          
            Initialize();
                      
        }

        public void Initialize()
        {
            try
            {
                // RxSocket is used to send selected test queue to the tester and to listen for incoming test queues if this machine is the tester
                rxSocket = new ReceiveSocket(testCollection, testCollectionMutex, this);

                // testLaunch is used to execute any tests in the local test queue if this machine is the tester
                testLaunch = new TestLaunch(testCollection, testCollectionMutex, this);

                // Get a reference to the tasks collection.
                _tasks = (Tasks)this.Resources["tasks"];
                _results = (TestResultss)this.Resources["results"];

                // read Test XML file to discover which tests are available to test
                activateTests(activateTestsOptions.parseTestFile);

                activateResults();

                string[] arguments = Environment.GetCommandLineArgs();
                System.Diagnostics.Debug.WriteLine("Command line: " + string.Join(" ", arguments.Select(v => v.ToString())));
                if (arguments.Length > 1 && arguments[1] != String.Empty)
                {
                    commandLineMode = true;
                    uploadOneBatch(arguments[1]);
                }
                string strloaded = "Found " + testNum + " tests";
                lblStatusBar.Content = strloaded;
                System.Diagnostics.Debug.WriteLine(strloaded);
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

                if (testLaunch != null)
                {
                    testLaunch.KillLaunchThread();
                }
                if (rxSocket != null)
                {
                    rxSocket.KillListenThread();
                }                
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("killConnectionToEmote: " + ex.ToString());
            }
        }

        private void closingTasks()
        {
            // saving tester paths
            saveSettings();
            Properties.Settings.Default.Save();

            // killing all running threads
            Kill();            
        }

        public void OnClosing(object sender, CancelEventArgs e)
        {
            closingTasks();
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

            // if we are running only one batch file from the command line then when all the tests are done we quit
            if ((_displayTestCollection.Count == 0) && (commandLineMode == true))
            {
                System.Diagnostics.Debug.WriteLine("No more tasks to run in command line mode. Quitting.");
                closingTasks();
                Environment.Exit(0);
            }
        }

        public void ClearTestItemsMethod()
        {
            _displayTestCollection.Clear();
        }

        private void OnTestTick(object source, EventArgs e) {
            if (listViewStatus.Items.Count <= 0)
                testTimer.Stop();

            DateTime now = DateTime.Now;
            TimeSpan ts = now.Subtract(dateTestTimerBegin);
            lblTestTimer.Content = ts.ToString(@"mm\:ss");
        }

        public void AutoSizeColumns()
        {
            GridView gv = listViewStatus.View as GridView;
            if (gv != null)
            {
                foreach (var c in gv.Columns)
                {
                    if (double.IsNaN(c.Width))
                    {
                        c.Width = c.ActualWidth;
                    }
                    c.Width = double.NaN;
                }
            }
        }

        public void DisplayUpdateMethod()
        {
            listViewStatus.Items.Refresh();
            listViewStatus.Items.Refresh();// lblStatusBar.Text = testStatus;//(listViewStatus.Items[0]);
            //AutoSizeColumns();
            lblStatusBar.Content = ((TestDescription)listViewStatus.Items[0]).testName + " | " + ((TestDescription)listViewStatus.Items[0]).testState;
            this.InvalidateVisual();
        }

        public void StartTestTimerMethod()
        {
            testTimer = new DispatcherTimer();
            testTimer.Interval = TimeSpan.FromSeconds(1);
            testTimer.Tick += new EventHandler(OnTestTick);
            testTimer.Start();
            dateTestTimerBegin = DateTime.Now;
        }

        public void StopTestTimerMethod() {
            if (testTimer != null)
                testTimer.Stop();
        }

        public void ClearStatusBar()
        {
            lblStatusBar.Content = "";
            lblTestTimer.Content = "";
            lblStatusBarError.Content = "";
        }

        public void PrintErrorMethod(string message)
        {
            string outError = "[" + DateTime.Now.ToString("HH:mm:ss") + "] ERROR: " + message;
            Console.WriteLine(outError);
            lblStatusBarError.Content = outError;
        }

        private void tbTestSourcePath_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Directory.Exists(tbTestSourcePath.Text) == true)
            {
                // if test source path changed then we will automatically enable/diable test options
                activateTests(activateTestsOptions.parseTestFile);

                // this string is used because other threads are not allowed to access the text box directly
                textTestSourcePath = tbTestSourcePath.Text;
            }
            else
            {
                if (_tasks == null)
                    return;
                _tasks.Clear();
                testNum = 1;
            }
            
        }

        private void activateTests(activateTestsOptions option)
        {
            try
            {
                if (_tasks == null)
                    return;
                _tasks.Clear();
                testNum = 1;
                availableTests = new TestDescription[1000];
                System.IO.DirectoryInfo xmlPath = new System.IO.DirectoryInfo(tbTestSourcePath.Text);
                WalkDirectoryTree(xmlPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Activate XML reading ended with exception: " + ex.Message);
            }
        }

        private void activateResults()
        {
            try
            {
                if (_results == null)
                    return;
                _results.Clear();
                resultNum = 1;
                availableTestResults = new TestResults[1000];
                System.IO.DirectoryInfo xmlReceiptPath = new System.IO.DirectoryInfo(tbTestReceiptPath.Text);
                WalkDirectoryForResults(xmlReceiptPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ActivateResults XML reading ended with exception: " + ex.Message);
            }
        }

        private static void WalkDirectoryForResults(System.IO.DirectoryInfo root) {
            System.IO.FileInfo[] files = null;
            try {
                files = root.GetFiles("*_receipt.xml");
            }
            catch (UnauthorizedAccessException e) {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }
            catch (System.IO.DirectoryNotFoundException e) {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }

            if (files != null) {
                foreach (System.IO.FileInfo fi in files) {
                    AddResult(fi);
                }
            }
        }

        private static void WalkDirectoryTree(System.IO.DirectoryInfo root)
        {
            System.IO.FileInfo[] files = null;
            System.IO.DirectoryInfo[] subDirs = null;

            // First, process all the files directly under this folder 
            try
            {
                files = root.GetFiles("*.*");
            }
            // This is thrown if even one of the files requires permissions greater 
            // than the application provides. 
            catch (UnauthorizedAccessException e)
            {
                // This code just writes out the message and continues to recurse. 
                // You may decide to do something different here. For example, you 
                // can try to elevate your privileges and access the file again.
                //log.Add(e.Message);
                System.Diagnostics.Debug.WriteLine(e.Message);
            }

            catch (System.IO.DirectoryNotFoundException e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }

            if (files != null)
            {
                foreach (System.IO.FileInfo fi in files)
                {
                    // In this example, we only access the existing FileInfo object. If we 
                    // want to open, delete or modify the file, then 
                    // a try-catch block is required here to handle the case 
                    // where the file has been deleted since the call to TraverseTree().     
              
                    // filtering out template test description file
                    if (!fi.FullName.Contains("Template"))
                    {
                        if ( fi.Name.Equals("tests.xml") )
                        {
                            System.Diagnostics.Debug.WriteLine(fi.FullName);
                            AddTests(fi);
                        }
                        else if (fi.Name.EndsWith(".batch"))
                        {
                            AddBatch(fi);
                        }
                    }
                }

                // Now find all the subdirectories under this directory.
                subDirs = root.GetDirectories();

                foreach (System.IO.DirectoryInfo dirInfo in subDirs)
                {
                    // Resursive call for each subdirectory.
                    WalkDirectoryTree(dirInfo);
                }
            }
        }

        private static void AddBatch(System.IO.FileInfo fi)
        {
            System.Diagnostics.Debug.WriteLine("adding batch file: " + fi.Name);
            try
            {
                using (StreamReader reader = new StreamReader(fi.FullName))
                {

                    string line;                                        
                    line = reader.ReadLine();  // Description
                    availableTests[testNum - 1] = null;
                    string batchName = fi.Name;
                    int index = batchName.LastIndexOf('.');
                    batchName = batchName.Substring(0, index);
                    index = fi.FullName.LastIndexOf(@"TestSuite");
                    string strippedPath = fi.FullName.Substring(index+10);
                    _tasks.Add(new Task()
                    {
                        TestNum = testNum,
                        Name = batchName,
                        Type = "Batch",
                        Path = strippedPath,
                        Description = line,
                        Selected = false
                    });
                    testNum++;
                    reader.Close();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("AddTests: " + ex.ToString());
            }
        }

        private static void AddResult(System.IO.FileInfo fi)
        {
            try {
                XmlSerializer serializer = new XmlSerializer(typeof(TestResults));
                Stream reader = new FileStream(fi.FullName, FileMode.Open);
                TestResults result = (TestResults)serializer.Deserialize(reader);

            availableTestResults[resultNum - 1] = result;
            _results.Add(result);
            resultNum++;
            }
            catch (Exception e) {
                Console.WriteLine(e.Message + " " + fi.FullName);
            }
        }

        private static void AddResult(TestReceipt rt)
        {
            using (Stream reader = new MemoryStream(Encoding.UTF8.GetBytes(rt.ToString())))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(TestResults));
                TestResults result = (TestResults)serializer.Deserialize(reader);

                availableTestResults[resultNum - 1] = result;
                _results.Add(result);
                resultNum++;
            }
        }

        private static void AddTests(System.IO.FileInfo fi){
            StreamReader testReader = new StreamReader(fi.FullName);
            try
            {
                int index = fi.FullName.LastIndexOf(@"TestSuite");
                string strippedPath = fi.FullName.Substring(index + 10);
                index = strippedPath.LastIndexOf('\\');
                strippedPath = strippedPath.Substring(0, index);
                // Create an XmlReader
                using (XmlReader reader = XmlReader.Create(testReader))
                {
                    System.Diagnostics.Debug.WriteLine("AddTests: Starting to read available test XML test file.");
                    while (reader != null)
                    {
                        // read in each defined test within the configuration file and activate the appropriate checkbox
                        TestDescription readTest = rxSocket.readXMLTest(reader);
                        if (readTest.testReadComplete == false)
                            return;

                        // if this is a test supporting a primary test then we don't list it (contains the words "support project" but not the word "load"). It will automatically be loaded by primary test if selected.
                        if ((readTest.testSupporting.Contains("load") == false) && (readTest.testSupporting.Contains("support project") == true))
                            return;

                        availableTests[testNum - 1] = readTest;
                        readTest.testPath = strippedPath;                            
                        _tasks.Add(new Task()
                        {
                            TestNum = testNum,
                            Name = readTest.testName,
                            Type = readTest.testType,
                            Path = readTest.testPath,
                            Description = readTest.testDescription,
                            Selected = false
                        });
                        testNum++;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("AddTests: " + ex.ToString());
            }
        }

        private void QueueSupportTest(string fileName, StreamWriter writer)
        {                
            try
            {
                StreamReader testReader = new StreamReader(textTestSourcePath + @"\" + fileName);
                // Create an XmlReader
                using (XmlReader reader = XmlReader.Create(testReader))
                {
                    System.Diagnostics.Debug.WriteLine("QueueSupportTest: Starting to read available test XML test file.");
                    while (reader != null)
                    {
                        // read in each defined test within the configuration file and activate the appropriate checkbox
                        TestDescription readTest = rxSocket.readXMLTest(reader);
                        if (readTest.testReadComplete == false)
                            return;

                        if (readTest.testerName == String.Empty)
                            readTest.testerName = System.Environment.UserName.ToString();
                        if (readTest.testLocation == String.Empty)
                            readTest.testLocation = System.Environment.MachineName.ToString();
                        if (readTest.testMFVersionNum == String.Empty)
                            readTest.testMFVersionNum = textMFSelected;
                        if (readTest.testGitOption == String.Empty)
                            readTest.testGitOption = textGitCodeLocation;
                        if (readTest.testGitBranch == String.Empty)
                            readTest.testGitBranch = textGitCodeBranch;
                        if (readTest.testHardware == String.Empty)
                            readTest.testHardware = textHardware;
                        if (readTest.testSolution == String.Empty)
                            readTest.testSolution = textSolution;
                        if (readTest.testMemoryType == String.Empty)
                            readTest.testMemoryType = textMemoryType;
                        if (readTest.testSolutionType == String.Empty)
                            readTest.testSolutionType = textSolutionType;
                        if (readTest.testGCCVersion == String.Empty)
                            readTest.testGCCVersion = textGCCVersion;
                        if (readTest.testJTAGHarness == String.Empty)
                            readTest.testJTAGHarness = textJTAGHarness;
                        if (readTest.testPowerAutomateSelected == String.Empty)
                            readTest.testPowerAutomateSelected = textPowerAutomateSelected;
                        if (readTest.testBuild == String.Empty)
                            readTest.testBuild = textCodeBuildSelected;
                        writer.Write(readTest.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("QueueSupportTest: " + ex.ToString());
                MainWindow.showMessageBox("Error reading file: " + textTestSourcePath + @"\" + fileName);
            }
        }

        private void UploadBatch(string path, StreamWriter writer)
        {
            // this function will read batch files and queue up any tests
            // this function is recursive if a batch file lists other batch files (we will only allow upto 10 batch calls to prevent endless batch loop call)            
            batchCall++;
            System.Diagnostics.Debug.WriteLine("Upload batch call: " + batchCall.ToString());
            if (batchCall >= 10)
                return;

            try
            {
                StreamReader reader = new StreamReader(textTestSourcePath + @"\" + path);
                string line;
                line = reader.ReadLine();  // Description
                line = reader.ReadLine();
                while (line != null)
                {
                    if (line.EndsWith(".batch"))
                    {
                        UploadBatch(line, writer);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine(line);
                        for (int testIndex = 1; testIndex <= testNum; testIndex++)
                        {
                            if (availableTests[testIndex - 1] != null)
                            {
                                if (availableTests[testIndex - 1].testPath.Equals(line))
                                {
                                    System.Diagnostics.Debug.WriteLine("queueing " + testIndex.ToString() + " with path " + line);
                                    UploadTask(testIndex, writer);
                                }
                            }
                        }
                    }
                    line = reader.ReadLine();
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("UploadBatch: " + ex.ToString());
            }
        }

        private void UploadTask(int passedTestNum, StreamWriter writer)
        {
            TestDescription tempTask = new TestDescription(availableTests[passedTestNum - 1]);
            // if the follow items are specified already we keep those parameters, otherwise those test parameters are populated here
            if (tempTask.testerName == String.Empty)
                tempTask.testerName = System.Environment.UserName.ToString();
            if (tempTask.testLocation == String.Empty)
                tempTask.testLocation = System.Environment.MachineName.ToString();
            if (tempTask.testMFVersionNum == String.Empty)
                tempTask.testMFVersionNum = textMFSelected;
            if (tempTask.testGitOption == String.Empty)
                tempTask.testGitOption = textGitCodeLocation;
            if (tempTask.testGitBranch == String.Empty)
                tempTask.testGitBranch = textGitCodeBranch;
            if (tempTask.testHardware == String.Empty)
                tempTask.testHardware = textHardware;
            if (tempTask.testSolution == String.Empty)
                tempTask.testSolution = textSolution;
            if (tempTask.testMemoryType == String.Empty)
                tempTask.testMemoryType = textMemoryType;
            if (tempTask.testSolutionType == String.Empty)
                tempTask.testSolutionType = textSolutionType;
            if (tempTask.testGCCVersion == String.Empty)
                tempTask.testGCCVersion = textGCCVersion;
            if (tempTask.testJTAGHarness == String.Empty)
                tempTask.testJTAGHarness = textJTAGHarness;
            if (tempTask.testPowerAutomateSelected == String.Empty)
                tempTask.testPowerAutomateSelected = textPowerAutomateSelected;
            if (tempTask.testBuild == String.Empty)
                tempTask.testBuild = textCodeBuildSelected;
            if (tempTask.testSupporting != String.Empty)
            {
                // queueing up support files
                string tempString;
                if (tempTask.testSupporting.StartsWith("load support projects")){
                    tempString = tempTask.testSupporting.Remove(0, 21);
                    int indexColon = tempString.IndexOf(':');
                    tempString = tempString.Remove(0, indexColon+1);
                    tempString = tempString.Trim();
                    string[] queueProjects = tempString.Split(' ');
                    int projectNum = queueProjects.Length;
                    for (int k = 0; k < projectNum; k++)
                    {
                        System.Diagnostics.Debug.WriteLine("support project: " + queueProjects[k]);
                        QueueSupportTest(queueProjects[k], writer);
                    }
                }
            }
            writer.Write(tempTask.ToString());
        }

        // this function is used to upload a batch file specified in the command line of the TestRig
        private void uploadOneBatch(string batchName)
        {            
            // clearing variable keeping track of batch calls (in case of endless loop)
            batchCall = 0;

            // upon click any tests selected will generate a test description in a file which will then be sent to the test machine (to localhost if this machine is the tester)
            string fileName = "test.config";
            using (StreamWriter writer = new StreamWriter(fileName))
            {
                writer.WriteLine("<TestSuite>");
                foreach (Task queueTask in _tasks)
                {
                    if (queueTask.Type.Equals("Batch")){                        
                        int index = batchName.LastIndexOf('.');
                        string strippedBatchName = batchName.Substring(0, index);
                        if (queueTask.Name.Equals(strippedBatchName))
                            UploadBatch(queueTask.Path, writer);  
                    }
                }
                writer.WriteLine("</TestSuite>");
            }
            // sending the generated test file to the test machine queue
            rxSocket.uploadTests();
        }

        private void Upload_Click(object sender, RoutedEventArgs e)
        {
            ClearStatusBar();
            // clearing variable keeping track of batch calls (in case of endless loop)
            batchCall = 0;

            // upon click any tests selected will generate a test description in a file which will then be sent to the test machine (to localhost if this machine is the tester)
            string fileName = "test.config";
            using (StreamWriter writer = new StreamWriter(fileName))
            {
                writer.WriteLine("<TestSuite>");
                foreach (Task queueTask in _tasks)
                {
                    if (queueTask.Selected == true)
                    {
                        if (queueTask.Type.Equals("Batch"))
                            UploadBatch(queueTask.Path, writer);
                        else
                            UploadTask(queueTask.TestNum, writer);
                    }
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
            if (((ComboBoxItem)cbMFVersionPath.SelectedItem).Content.ToString().Equals("MF 4.0"))
                textMFPath_4_0 = tbMFPath.Text;
            else
                textMFPath_4_3 = tbMFPath.Text;
        }

        private void tbTestReceiptPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Directory.Exists(tbTestSourcePath.Text) == true) {
                // if test source path changed then we will automatically enable/diable test options
                activateResults();

                // this string is used because other threads are not allowed to access the text box directly
                textTestReceiptPath = tbTestReceiptPath.Text;
            }
            else {
                if (_results == null)
                    return;
                _results.Clear();
                resultNum = 1;
            }
        }

        private void tbOCDInterface_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (((ComboBoxItem)cbInterface.SelectedItem).Content.ToString().Equals("Primary"))
                textOCDInterfacePrimary = tbOCDInterface.Text;
            else
                textOCDInterfaceSecondary1 = tbOCDInterface.Text;
        }

        private void tbOCDTarget_TextChanged(object sender, TextChangedEventArgs e)
        {
            textOCDTarget = tbOCDTarget.Text;
        }

        private void tbOCDExe_TextChanged(object sender, TextChangedEventArgs e)
        {
            textOCDExe = tbOCDExe.Text;
        }

        private void tbGitPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            textGitPath = tbGitPath.Text;
        }

        private void tbCodeBranch_TextChanged(object sender, TextChangedEventArgs e)
        {
            textGitCodeBranch = tbCodeBranch.Text;
        }

        private void cbMFVersionPath_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (settingsInitialized == false)
            {
                // buttons have not yet been defined by InitializeComponent
                return;
            }
            textMFPathSelection = ((ComboBoxItem)cbMFVersionPath.SelectedItem).Content.ToString();
            if (textMFPathSelection.Equals("MF 4.0"))
                tbMFPath.Text = textMFPath_4_0;
            else
                tbMFPath.Text = textMFPath_4_3;

            checkPaths();
        }   

        public static void showMessageBox(string message)
        {
            MessageBox.Show(message);
        }

        private void buttonDebug_Click(object sender, RoutedEventArgs e)
        {
            testLaunch.DebugFunction();  
        }

        private void checkPaths()
        {
            try
            {
                if (File.Exists(tbOCDExe.Text) == false)
                {
                    MessageBox.Show("OpenOCD executable: " + tbOCDExe.Text + " does not exist.");
                    return;
                }

                if (File.Exists(tbOCDInterface.Text) == false)
                {
                    MessageBox.Show("OpenOCD Interface file: " + tbOCDInterface.Text + " does not exist.");
                    return;
                }

                if (File.Exists(tbOCDTarget.Text) == false)
                {
                    MessageBox.Show("OpenOCD Target file: " + tbOCDTarget.Text + " does not exist.");
                    return;
                }

                if (Directory.Exists(tbBuildSourceryPath.Text) == false)
                {
                    MessageBox.Show("Codesourcery path: " + tbBuildSourceryPath.Text + " does not exist.");
                    return;
                }

                if (Directory.Exists(tbMFPath.Text) == false)
                {
                    MessageBox.Show("Microframework path: " + tbMFPath.Text + " does not exist.");
                    return;
                }

                if (Directory.Exists(tbGitPath.Text) == false)
                {
                    MessageBox.Show("Test tool path: " + tbGitPath.Text + " does not exist.");
                    return;
                }

                if (Directory.Exists(tbTestSourcePath.Text) == false)
                {
                    MessageBox.Show("Test source path: " + tbTestSourcePath.Text + " does not exist.");
                    return;
                }

                if (Directory.Exists(tbGitPath.Text) == false)
                {
                    MessageBox.Show("Git Hub path: " + tbGitPath.Text + " does not exist.");
                    return;
                }

                if (Directory.Exists(tbTestReceiptPath.Text) == false)
                {
                    MessageBox.Show("Test Receipt path: " + tbTestReceiptPath.Text + " does not exist.");
                    return;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("readSettings: " + ex.ToString());
            }
        }

        private void readSettings()
        {
            try
            {
                // checking to make sure paths actually exist                
                tbOCDExe.Text = Properties.Settings.Default.OCDExe.ToString();
                textOCDInterfacePrimary = Properties.Settings.Default.OCDInterfacePrimary.ToString();
                textOCDInterfaceSecondary1 = Properties.Settings.Default.OCDInterfaceSecondary1.ToString();
                if (((ComboBoxItem)cbInterface.SelectedItem).Content.ToString().Equals("Primary"))
                    tbOCDInterface.Text = textOCDInterfacePrimary;
                else
                    tbOCDInterface.Text = textOCDInterfaceSecondary1;
                tbOCDTarget.Text = Properties.Settings.Default.OCDTarget.ToString();
                tbBuildSourceryPath.Text = Properties.Settings.Default.CSPath.ToString();

                cbCOMPort.SelectedIndex = Properties.Settings.Default.COMPortPrimary;

                cbMFVersionPath.SelectedIndex = Properties.Settings.Default.MFPathSelection;    
                textMFPath_4_0 = Properties.Settings.Default.MFPath_4_0.ToString();
                textMFPath_4_3 = Properties.Settings.Default.MFPath_4_3.ToString();
                if (((ComboBoxItem)cbMFVersionPath.SelectedItem).Content.ToString().Equals("MF 4.0"))
                    tbMFPath.Text = textMFPath_4_0;
                else
                    tbMFPath.Text = textMFPath_4_3;
                tbGitPath.Text = Properties.Settings.Default.GitPath.ToString();
                tbTestSourcePath.Text = Properties.Settings.Default.TSPath.ToString();
                tbTestReceiptPath.Text = Properties.Settings.Default.TRPath.ToString();

                cbHardware.SelectedIndex = Properties.Settings.Default.HSSelection;
                cbMemory.SelectedIndex = Properties.Settings.Default.MTSelection;
                cbSolutionType.SelectedIndex = Properties.Settings.Default.STSelection;
                cbGCCVersion.SelectedIndex = Properties.Settings.Default.GVSelection;

                COMPortSelectionPrimary = Properties.Settings.Default.COMPortPrimary;
                COMPortSelectionSecondary1 = Properties.Settings.Default.COMPortSecondary1;
                cbMFSelected.SelectedIndex = Properties.Settings.Default.MFSelection;
                if (((ComboBoxItem)cbInterface.SelectedItem).Content.ToString().Equals("Primary"))
                {                    
                    cbCOMPort.SelectedIndex = COMPortSelectionPrimary;                    
                }
                else
                {
                    cbCOMPort.SelectedIndex = COMPortSelectionSecondary1;                    
                }
                textCOMPortPrimary = ((ComboBoxItem)cbCOMPort.Items[COMPortSelectionPrimary]).Content.ToString();
                textCOMPortPrimary = textCOMPortPrimary.Remove(3, 1);
                textCOMPortSecondary1 = ((ComboBoxItem)cbCOMPort.Items[COMPortSelectionSecondary1]).Content.ToString();
                textCOMPortSecondary1 = textCOMPortSecondary1.Remove(3, 1);

                cbPowerAutomate.IsChecked = Properties.Settings.Default.PowerCycleAutomated;
                if (cbPowerAutomate.IsChecked == true)
                    textPowerAutomateSelected = true.ToString();
                else
                    textPowerAutomateSelected = false.ToString();

                settingsInitialized = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("readSettings: " + ex.ToString());
            }
        }

        private void saveSettings()
        {
            try
            {
                // paths are saved to a settings file
                Properties.Settings.Default["OCDInterfacePrimary"] = textOCDInterfacePrimary;
                Properties.Settings.Default["OCDInterfaceSecondary1"] = textOCDInterfaceSecondary1;
                Properties.Settings.Default["OCDTarget"] = tbOCDTarget.Text;
                Properties.Settings.Default["OCDExe"] = tbOCDExe.Text;
                Properties.Settings.Default["CSPath"] = tbBuildSourceryPath.Text;
                Properties.Settings.Default["MFPath_4_0"] = textMFPath_4_0;
                Properties.Settings.Default["MFPath_4_3"] = textMFPath_4_3;
                Properties.Settings.Default["MFPathSelection"] = cbMFVersionPath.SelectedIndex;
                Properties.Settings.Default["COMPortPrimary"] = COMPortSelectionPrimary;
                Properties.Settings.Default["COMPortSecondary1"] = COMPortSelectionSecondary1;
                Properties.Settings.Default["GitPath"] = tbGitPath.Text;
                Properties.Settings.Default["TSPath"] = tbTestSourcePath.Text;
                Properties.Settings.Default["TRPath"] = tbTestReceiptPath.Text;
                Properties.Settings.Default["HSSelection"] = cbHardware.SelectedIndex;
                Properties.Settings.Default["MTSelection"] = cbMemory.SelectedIndex;
                Properties.Settings.Default["STSelection"] = cbSolutionType.SelectedIndex;
                Properties.Settings.Default["GVSelection"] = cbGCCVersion.SelectedIndex;
                Properties.Settings.Default["MFSelection"] = cbMFSelected.SelectedIndex;
                Properties.Settings.Default["PowerCycleAutomated"] = cbPowerAutomate.IsChecked;
                
                Properties.Settings.Default.Save();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("saveToRegistry: " + ex.ToString());
            }
        }

        private void cbCodeLocation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (settingsInitialized == false)
            {
                // buttons have not yet been defined by InitializeComponent
                return;
            }
            textGitCodeLocation = ((ComboBoxItem)cbCodeLocation.SelectedItem).Content.ToString();
            if (textGitCodeLocation.Equals("Use local code"))
            {
                // read Test XML file to discover which tests are available to test
                activateTests(activateTestsOptions.parseTestFile);

                lblBranch.Visibility = Visibility.Hidden;
                tbCodeBranch.Visibility = Visibility.Hidden;
            }
            else if (textGitCodeLocation.Equals("Use archive code"))
            {
                activateTests(activateTestsOptions.allTests);
                lblBranch.Visibility = Visibility.Hidden;
                tbCodeBranch.Visibility = Visibility.Hidden;
            }
            else if (textGitCodeLocation.Equals("Use archive branch code"))
            {
                activateTests(activateTestsOptions.allTests);
                lblBranch.Visibility = Visibility.Visible;
                tbCodeBranch.Visibility = Visibility.Visible;
            }
        }

        private void CollectionViewSource_Filter(object sender, FilterEventArgs e)
        {
            Task t = e.Item as Task;
            if (t != null)
                if ((t.Name.Contains(tbNameFilter.Text) == true) && (t.Path.Contains(tbPathFilter.Text) == true) && (t.Type.Contains(tbTypeFilter.Text) == true) && (t.Description.Contains(tbDescFilter.Text) == true))
                    e.Accepted = true;
                else
                    e.Accepted = false;

        }

        private void selectTests_Checked(object sender, RoutedEventArgs e)
        {
            foreach (Task t in _tasks)
            {
                if ((t.Name.Contains(tbNameFilter.Text) == true) && (t.Path.Contains(tbPathFilter.Text) == true) && (t.Type.Contains(tbTypeFilter.Text) == true) && (t.Description.Contains(tbDescFilter.Text) == true))
                {
                    //System.Diagnostics.Debug.WriteLine(t.Name.ToString());
                    if (selectTests.IsChecked == true)
                        t.Selected = true;
                    else
                        t.Selected = false;
                }                
            }
        } 

        private void DataGridCell_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DataGridCell cell = sender as DataGridCell;
            if (cell != null && !cell.IsEditing && !cell.IsReadOnly)
            {
                if (!cell.IsFocused)
                {
                    cell.Focus();
                }
                DataGrid dataGrid = FindVisualParent<DataGrid>(cell);
                if (dataGrid != null)
                {
                    if (dataGrid.SelectionUnit != DataGridSelectionUnit.FullRow)
                    {
                        if (!cell.IsSelected)
                            cell.IsSelected = true;
                    }
                    else
                    {
                        DataGridRow row = FindVisualParent<DataGridRow>(cell);
                        if (row != null && !row.IsSelected)
                        {
                            row.IsSelected = true;
                        }
                    }
                }
            }
        }

        static T FindVisualParent<T>(UIElement element) where T : UIElement
        {
            UIElement parent = element;
            while (parent != null)
            {
                T correctlyTyped = parent as T;
                if (correctlyTyped != null)
                {
                    return correctlyTyped;
                }

                parent = VisualTreeHelper.GetParent(parent) as UIElement;
            }
            return null;
        }

        private void testDataGrid_AutoGeneratedColumns(object sender, EventArgs e)
        {
            testDataGrid.Columns[0].Visibility = Visibility.Hidden;
            testDataGrid.Columns[1].Width = 125;
            testDataGrid.Columns[2].Width = 100;
            testDataGrid.Columns[3].Width = 250;
            testDataGrid.Columns[4].Width = 345;
            testDataGrid.Columns[1].IsReadOnly = true;
            testDataGrid.Columns[2].IsReadOnly = true;
            testDataGrid.Columns[3].IsReadOnly = true;
            testDataGrid.Columns[4].IsReadOnly = true;
        }

        private void tbFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(testDataGrid.ItemsSource).Refresh();
        }

        private void cbHardware_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (cbHardware.SelectedIndex)
            {
                case 0:
                    textHardware = "Emote v1";
                    textSolution = "STM32F10x";
                    textJTAGHarness = "Olimex";
                    break;
                case 1:
                    textHardware = "Emote.Now";
                    textSolution = "EmoteDotNow";
                    textJTAGHarness = "Olimex";
                    break;
                case 2:
                    textHardware = "Soc8200";
                    textSolution = "SOC8200";
                    textJTAGHarness = "Olimex";
                    break;
                case 3:
                    textHardware = "Adapt";
                    textSolution = "SOC_ADAPT";
                    textJTAGHarness = "fastboot";
                    break;
                case 4:
                    textHardware = "Adapt";
                    textSolution = "SOC_ADAPT";
                    textJTAGHarness = "Lauterbach";
                    break;
                default:
                    textHardware = "Emote v1";
                    textSolution = "STM32F10x";
                    textJTAGHarness = "Olimex";
                    break;
            }
        }

        private void cbMemory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (cbMemory.SelectedIndex)
            {
                case 0:
                    textMemoryType = "FLASH";
                    break;
                case 1:
                    textMemoryType = "RAM";
                    break;
                case 2:
                    textMemoryType = "External FLASH";
                    break;
                default:
                    textMemoryType = "FLASH";
                    break;
            }
        }

        private void cbSolutionType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (cbSolutionType.SelectedIndex)
            {
                case 0:
                    textSolutionType = "TinyCLR";
                    break;
                case 1:
                    textSolutionType = "TinyBooter";
                    break;
                default:
                    textSolutionType = "TinyCLR";
                    break;
            }
        }

        private void cbGCCVersion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (cbGCCVersion.SelectedIndex)
            {
                case 0:
                    textGCCVersion = "GCC4.2";
                    break;
                case 1:
                    textGCCVersion = "GCC4.4";
                    break;
                case 2:
                    textGCCVersion = "GCC4.7";
                    break;
                default:
                    textGCCVersion = "GCC4.7";
                    break;
            }
        }

        private void cbMFSelected_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (cbMFSelected.SelectedIndex)
            {
                case 0:
                    textMFSelected = "4.0";
                    break;
                case 1:
                    textMFSelected = "4.3";
                    break;
                case 2:
                    textMFSelected = "4.3";
                    break;
                default:
                    textMFSelected = "4.3";
                    break;
            }
            switch (cbMFSelected.SelectedIndex)
            {
                case 0:
                    textCodeBuildSelected = "Debug";
                    break;
                case 1:
                    textCodeBuildSelected = "Debug";
                    break;
                case 2:
                    textCodeBuildSelected = "Release";
                    break;
                default:
                    textCodeBuildSelected = "Debug";
                    break;
            }
        }

        private void cbCOMPort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (settingsInitialized == false)
            {
                // buttons have not yet been defined by InitializeComponent
                return;
            }
            if (((ComboBoxItem)cbInterface.SelectedItem).Content.ToString().Equals("Primary"))
            {
                COMPortSelectionPrimary = cbCOMPort.SelectedIndex;
                textCOMPortPrimary = ((ComboBoxItem)cbCOMPort.SelectedItem).Content.ToString();
                textCOMPortPrimary = textCOMPortPrimary.Remove(3, 1);
            }
            else
            {
                COMPortSelectionSecondary1 = cbCOMPort.SelectedIndex;
                textCOMPortSecondary1 = ((ComboBoxItem)cbCOMPort.SelectedItem).Content.ToString();
                textCOMPortSecondary1 = textCOMPortSecondary1.Remove(3, 1);
            }            
        }

        private void cbPowerAutomate_Unchecked(object sender, RoutedEventArgs e)
        {
            textPowerAutomateSelected = false.ToString();
        }

        private void cbPowerAutomate_Checked(object sender, RoutedEventArgs e)
        {
            textPowerAutomateSelected = true.ToString();
        }

        private void cbInterface_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (settingsInitialized == false)
            {
                // buttons have not yet been defined by InitializeComponent
                return;
            }
            if (((ComboBoxItem)cbInterface.SelectedItem).Content.ToString().Equals("Primary"))
            {
                tbOCDInterface.Text = textOCDInterfacePrimary;
                cbCOMPort.SelectedIndex = COMPortSelectionPrimary;
            }
            else
            {
                tbOCDInterface.Text = textOCDInterfaceSecondary1;
                cbCOMPort.SelectedIndex = COMPortSelectionSecondary1;     
            }

            checkPaths();
        }

        private void btnBatch_Click(object sender, RoutedEventArgs e)
        {
            BatchDlg batchDialog = new BatchDlg();
            System.Windows.Forms.Application.Run(batchDialog);
            if (batchDialog.DialogResult == System.Windows.Forms.DialogResult.OK)
            {
                System.Diagnostics.Debug.WriteLine("dialog results: " + batchDialog.batchName + " " + batchDialog.batchDesc);
                string fullPath = textTestSourcePath;
                int index = fullPath.LastIndexOf(@"TestSuite");
                string strippedPath = fullPath.Substring(0, index);
                strippedPath = strippedPath + @"TestSuite\Batch Files\";

                // upon click any tests selected will be added to a batch file
                string fileName = strippedPath + batchDialog.batchName + ".batch";
                using (StreamWriter writer = new StreamWriter(fileName))
                {
                    writer.WriteLine(batchDialog.batchDesc);
                    foreach (Task queueTask in _tasks)
                    {
                        if (queueTask.Selected == true)
                        {
                            TestDescription tempTask = new TestDescription(availableTests[queueTask.TestNum - 1]);
                            writer.WriteLine(tempTask.testPath);
                        }
                    }
                    writer.Close();
                }
                activateTests(activateTestsOptions.parseTestFile);
            }
        }

        private void btnAbortTests_Click(object sender, RoutedEventArgs e)
        {
            testLaunch.AbortTests();
        }       
        }

        private void CollectionViewResults_Filter(object sender, FilterEventArgs e)
        {
            TestResults t = e.Item as TestResults;
            if (t != null)
                if ((t.TestResult.Contains(tbResultsFilter.Text) == true))
                    e.Accepted = true;
                else
                    e.Accepted = false;

        }

        private void tbResultsFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(resultDataGrid.ItemsSource).Refresh();
        }
    
    }
    class TestPassToColorConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            System.Diagnostics.Debug.WriteLine(value);
            if (value is string && (string)value != "True")
            {
                return Brushes.Red;
            }
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
