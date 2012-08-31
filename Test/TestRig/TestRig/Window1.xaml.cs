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

namespace TestRig
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        private OpenOCD vOpenOCD;

        private TestStatus tStatus;

        public string outTestStatus;

        public static System.Security.SecureString password;

        private LogicAnalyser vLogic;

        public static ObservableCollection<Test> _testCollection;

        public Window1()
        {
            //vLogic = Logic.Instance;
            //vLogic.Start();
            password = new System.Security.SecureString();
            //vmsbuild = msbuild.Instance;
            vOpenOCD = OpenOCD.Instance;
            tStatus = TestStatus.Instance;
            //outTestStatus = vmsbuild.OutMessages;
            _testCollection = new ObservableCollection<Test>();
            DataContext = _testCollection;
            //this.DataContext = this;
            InitializeComponent();           
        }

        private void textBox1_TextChanged(object sender, TextChangedEventArgs e)
        {
            vOpenOCD.Intrfce = textBox1.Text;
        }

        private void textBox2_TextChanged(object sender, TextChangedEventArgs e)
        {
            vOpenOCD.Target = textBox2.Text;
        }

        private void textBox3_TextChanged(object sender, TextChangedEventArgs e)
        {
            vOpenOCD.Exe = textBox3.Text;
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

            t.testerName = nameOfTester.Text;
            t.testState = "QUEUED";
            t.testName = testNameBox.Text;
            t.testLocation = RigSelect.Text;
            t.testPath = testPthBox.Text;
            t.buildProj = buildProjBox.Text;

            _testCollection.Add(t);
            tStatus.addTest(t);
            
           
        }

        private void textBox6_TextChanged(object sender, TextChangedEventArgs e)
        {
            tStatus.codeSourceryPath = textBox6.Text;
        }

        private void textPathBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            //vmsbuild.codeSourceryPath = textBox6.Text;
        }

        private void mfPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            tStatus.mfInstallationPath = mfPath.Text;
        }

        private void testPathBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            
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

        private void textBox4_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void stopLogic_Click(object sender, RoutedEventArgs e)
        {
            //vLogic.ReadStop();
        }


    }
}
