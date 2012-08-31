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
using System.Collections.ObjectModel;

namespace TestRigServer
{
    /// <summary>
    /// Interaction logic for Page1.xaml
    /// </summary>
    public partial class Page1 : Page
    {
        private OpenOCD vOpenOCD;

        private msbuild vmsbuild;

        private TestStatus tStatus;

        public string outTestStatus;

        public static ObservableCollection<Test> _testCollection;

        public Page1()
        {
            vmsbuild = msbuild.Instance;
            vOpenOCD = OpenOCD.Instance;
            tStatus = TestStatus.Instance;
            outTestStatus = vmsbuild.OutMessages;
            _testCollection = new ObservableCollection<Test>();
            DataContext = _testCollection;
            InitializeComponent();
        }

        private void OpenOCDInterfaceBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            vOpenOCD.Intrfce = OpenOCDInterfaceBox.Text;
        }

        private void OpenOCDTargetBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            vOpenOCD.Target = OpenOCDTargetBox.Text;
        }

        private void OpenOCDexeBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            vOpenOCD.Exe = OpenOCDexeBox.Text;
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            vOpenOCD.start();
        }

        private void Show_Click(object sender, RoutedEventArgs e)
        {
            outBox.Text += "";
            outBox.Text += vOpenOCD.ErrMessages;
        }

        private void Hide_Click(object sender, RoutedEventArgs e)
        {
            outBox.Visibility = Visibility.Hidden;
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Upload_Click(object sender, RoutedEventArgs e)
        {
            Test t = new Test();

            t.testerName = "Nived";
            t.testState = "QUEUED";
            t.testName = testNameBox.Text;
            t.testLocation = "localRig";
            t.testPath = testPthBox.Text;
            t.buildProj = buildProjBox.Text;

            _testCollection.Add(t);
            tStatus.addTest(t);

        }

        private void textBox6_TextChanged(object sender, TextChangedEventArgs e)
        {
            vmsbuild.codeSourceryPath = textBox6.Text;
        }

        private void mfPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            vmsbuild.mfInstalltionPath = mfPath.Text;
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {

        }

        
    }
}
