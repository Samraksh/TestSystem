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
    // Task Class // Requires using System.ComponentModel; 
    public class Task : INotifyPropertyChanged, IEditableObject
    {
        // The Task class implements INotifyPropertyChanged and IEditableObject  
        // so that the datagrid can properly respond to changes to the  
        // data collection and edits made in the DataGrid. 
        // Private task data. 
        private int m_TestNum = 0;
        private string m_ProjectName = string.Empty;
        private string m_ProjectType = string.Empty;
        private string m_ProjectPath = string.Empty;
        private string m_ProjectDesc = string.Empty;
        private bool m_Selected = false;

        // Data for undoing canceled edits. 
        private Task temp_Task = null;
        private bool m_Editing = false;

        // Public properties. 
        public int TestNum
        {
            get { return this.m_TestNum; }
            set
            {
                if (value != this.m_TestNum)
                {
                    this.m_TestNum = value;
                    NotifyPropertyChanged("TestNum");
                }
            }
        }

        public string Name
        {
            get { return this.m_ProjectName; }
            set
            {
                if (value != this.m_ProjectName)
                {
                    this.m_ProjectName = value;
                    NotifyPropertyChanged("Project Name");
                }
            }
        }

        public string Type
        {
            get { return this.m_ProjectType; }
            set
            {
                if (value != this.m_ProjectType)
                {
                    this.m_ProjectType = value;
                    NotifyPropertyChanged("Project Type");
                }
            }
        }

        public string Path
        {
            get { return this.m_ProjectPath; }
            set
            {
                if (value != this.m_ProjectPath)
                {
                    this.m_ProjectPath = value;
                    NotifyPropertyChanged("Project Path");
                }
            }
        }

        public string Description
        {
            get { return this.m_ProjectDesc; }
            set
            {
                if (value != this.m_ProjectDesc)
                {
                    this.m_ProjectDesc = value;
                    NotifyPropertyChanged("Project Description");
                }
            }
        }

        public bool Selected
        {
            get { return this.m_Selected; }
            set
            {
                if (value != this.m_Selected)
                {
                    this.m_Selected = value;
                    NotifyPropertyChanged("Selected");
                }
            }
        }

        // Implement INotifyPropertyChanged interface. 
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        // Implement IEditableObject interface. 
        public void BeginEdit()
        {
            if (m_Editing == false)
            {
                temp_Task = this.MemberwiseClone() as Task;
                m_Editing = true;
            }
        }

        public void CancelEdit()
        {
            if (m_Editing == true)
            {
                this.TestNum = temp_Task.TestNum;
                this.Name = temp_Task.Name;
                this.Type = temp_Task.Type;
                this.Path = temp_Task.Path;
                this.Description = temp_Task.Description;
                this.Selected = temp_Task.Selected;
                m_Editing = false;
            }
        }

        public void EndEdit()
        {
            if (m_Editing == true)
            {
                temp_Task = null;
                m_Editing = false;
            }
        }
    }
    // Requires using System.Collections.ObjectModel; 
    public class Tasks : ObservableCollection<Task>
    {
        // Creating the Tasks collection in this way enables data binding from XAML.
    }

    [ValueConversion(typeof(Boolean), typeof(String))]
    public class CompleteConverter : IValueConverter
    {
        // This converter changes the value of a Tasks Complete status from true/false to a string value of // "Complete"/"Active" for use in the row group header.
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool complete = (bool)value;
            if (complete)
                return "Selected";
            else
                return "Unselected";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string strComplete = (string)value;
            if (strComplete == "Selected")
                return true;
            else
                return false;
        }
    }
}
