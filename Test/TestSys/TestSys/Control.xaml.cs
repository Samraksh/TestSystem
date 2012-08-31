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

namespace TestSys
{
    /// <summary>
    /// Interaction logic for Control.xaml
    /// </summary>
    public partial class Control : UserControl
    {
        OpenOCD vInstance;

        public Control()
        {
            vInstance = OpenOCD.Instance;
            InitializeComponent();
        }

        private void textBox1_TextChanged(object sender, TextChangedEventArgs e)
        {
            vInstance.Intrfce = e.Changes.ToString();
        }

        private void textBox2_TextChanged(object sender, TextChangedEventArgs e)
        {
            vInstance.Target = e.Changes.ToString();
        }

        
    }
}
