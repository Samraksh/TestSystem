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
        private static ReceiveSocket rxSocket;
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
        public string textMFPath_4_0;
        public string textMFPath_4_3;
        public string textTestSourcePath;
        public string textTestReceiptPath;
        public string textOCDInterface;
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
        public static string textGCCVersion;
        public static string textMFSelected;
        public static TestDescription[] availableTests;
        private static Tasks _tasks;
        private static int testNum;
        private bool settingsInitialized = false;

        public MainWindow()
        {
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

                // Get a reference to the tasks collection.
                _tasks = (Tasks)this.Resources["tasks"];

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
            this.InvalidateVisual();
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
                    if (fi.Name.Equals("tests.xml") && !fi.FullName.Contains("Template") )
                    {
                        System.Diagnostics.Debug.WriteLine(fi.FullName);
                        AddTests(fi);
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
        
        private static void AddTests(System.IO.FileInfo fi){
            StreamReader testReader = new StreamReader(fi.FullName);
            try
            {
                // Create an XmlReader
                using (XmlReader reader = XmlReader.Create(testReader))
                {
                    System.Diagnostics.Debug.WriteLine("Starting to read available test XML test file.");
                    while (reader != null)
                    {
                        // read in each defined test within the configuration file and activate the appropriate checkbox
                        TestDescription readTest = rxSocket.readXMLTest(reader);
                        if (readTest.testReadComplete == false)
                            return;                        

                        availableTests[testNum - 1] = readTest;
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

        private void Upload_Click(object sender, RoutedEventArgs e)
        {
            // upon click any tests selected will generate a test description in a file which will then be sent to the test machine (to localhost if this machine is the tester)
            string fileName = "test.config";
            using (StreamWriter writer = new StreamWriter(fileName))
            {
                writer.WriteLine("<TestSuite>");
                foreach (Task queueTask in _tasks)
                {
                    if (queueTask.Selected == true)
                    {
                        TestDescription tempTask = new TestDescription(availableTests[queueTask.TestNum - 1]);
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
                        writer.Write(tempTask.ToString());
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
            if (cbMFVersionPath.SelectedValue.ToString().Contains("MF 4.0"))
                textMFPath_4_0 = tbMFPath.Text;
            else
                textMFPath_4_3 = tbMFPath.Text;
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
            textMFPathSelection = cbMFVersionPath.SelectedItem.ToString();
            if (textMFPathSelection.Contains("MF 4.0"))
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
                tbOCDInterface.Text = Properties.Settings.Default.OCDInterface.ToString();
                tbOCDTarget.Text = Properties.Settings.Default.OCDTarget.ToString();
                tbBuildSourceryPath.Text = Properties.Settings.Default.CSPath.ToString();

                cbMFVersionPath.SelectedIndex = Properties.Settings.Default.MFPathSelection;    
                textMFPath_4_0 = Properties.Settings.Default.MFPath_4_0.ToString();
                textMFPath_4_3 = Properties.Settings.Default.MFPath_4_3.ToString();
                if (cbMFVersionPath.SelectedValue.ToString().Contains("MF 4.0"))
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

                cbMFSelected.SelectedIndex = Properties.Settings.Default.MFSelection;

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
                Properties.Settings.Default["OCDInterface"] = tbOCDInterface.Text;
                Properties.Settings.Default["OCDTarget"] = tbOCDTarget.Text;
                Properties.Settings.Default["OCDExe"] = tbOCDExe.Text;
                Properties.Settings.Default["CSPath"] = tbBuildSourceryPath.Text;
                Properties.Settings.Default["MFPath_4_0"] = textMFPath_4_0;
                Properties.Settings.Default["MFPath_4_3"] = textMFPath_4_3;
                Properties.Settings.Default["MFPathSelection"] = cbMFVersionPath.SelectedIndex;
                Properties.Settings.Default["GitPath"] = tbGitPath.Text;
                Properties.Settings.Default["TSPath"] = tbTestSourcePath.Text;
                Properties.Settings.Default["TRPath"] = tbTestReceiptPath.Text;
                Properties.Settings.Default["HSSelection"] = cbHardware.SelectedIndex;
                Properties.Settings.Default["MTSelection"] = cbMemory.SelectedIndex;
                Properties.Settings.Default["STSelection"] = cbSolutionType.SelectedIndex;
                Properties.Settings.Default["GVSelection"] = cbGCCVersion.SelectedIndex;
                Properties.Settings.Default["MFSelection"] = cbMFSelected.SelectedIndex;
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
            textGitCodeLocation = cbCodeLocation.SelectedItem.ToString();
            if (textGitCodeLocation.Contains("Use local code"))
            {
                // read Test XML file to discover which tests are available to test
                activateTests(activateTestsOptions.parseTestFile);

                lblBranch.Visibility = Visibility.Hidden;
                tbCodeBranch.Visibility = Visibility.Hidden;
            }
            else if (textGitCodeLocation.Contains("Use archive code"))
            {
                activateTests(activateTestsOptions.allTests);
                lblBranch.Visibility = Visibility.Hidden;
                tbCodeBranch.Visibility = Visibility.Hidden;
            }
            else if (textGitCodeLocation.Contains("Use archive branch code"))
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
            testDataGrid.Columns[1].Width = 100;
            testDataGrid.Columns[2].Width = 50;
            testDataGrid.Columns[3].Width = 200;
            testDataGrid.Columns[4].Width = 200;
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
                    break;
                case 1:
                    textHardware = "Emote.Now";
                    textSolution = "EmoteDotNow";
                    break;
                case 2:
                    textHardware = "Soc8200";
                    textSolution = "SOC8200";
                    break;
                case 3:
                    textHardware = "Adapt";
                    textSolution = "ADAPT";
                    break;
                default:
                    textHardware = "Emote v1";
                    textSolution = "STM32F10x";
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
                default:
                    textGCCVersion = "GCC4.2";
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
                default:
                    textMFSelected = "4.3";
                    break;
            }
            
        }  
    }
}
