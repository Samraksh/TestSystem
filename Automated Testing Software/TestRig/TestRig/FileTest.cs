using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Collections.ObjectModel;
using System.Xml;
using System.Windows.Controls;

namespace TestRig
{
    class FileTest
    {
        public MainWindow mainHandle;
        private Thread RxThread;
        private static string accumReceiveString;
        private static bool saveToFile;
        private static string saveFileName;
        private static System.IO.StreamWriter saveDataFile;

        public FileTest(MainWindow passedHandle, TestDescription currentTest)
        {
            mainHandle = passedHandle;
            saveToFile = false;
        }
        
        public bool Save(string SendString)
        {
            if (saveToFile == true)
            {
                System.Diagnostics.Debug.WriteLine("Writing: " + SendString + " to file.");
                saveDataFile.Write(SendString);
            }
            else
                return false;

            return true;
        }

        public bool SendFile(string FileName)
        {
            if (saveToFile == true)
            {
                StreamReader sr;
                string line;

                sr = new StreamReader(FileName);
                line = sr.ReadLine();
                while (line != null)
                {
                    System.Diagnostics.Debug.WriteLine("Saving to file: " + line);
                    saveDataFile.WriteLine(line);
                    line = sr.ReadLine();
                }
            }
            else 
                return false;

            return true;
        }

        public bool Compare(string file1, string file2)
        {
            StreamReader first = new StreamReader(file1);
            StreamReader second = new StreamReader(file2);
            string line1, line2;
            bool filesEqual = true;

            line1 = first.ReadLine();
            line2 = second.ReadLine();
            while (line1 != null)
            {
                if (line1.Equals(line2) == false)
                {
                    System.Diagnostics.Debug.WriteLine("line " + line1 + " does not equal " + line2);
                    filesEqual = false;
                }

                line1 = first.ReadLine();
                line2 = second.ReadLine();
            }

            first.Close();
            second.Close();
            return filesEqual;            
        }

        public bool SaveToFile(string saveEnabled, string FileName)
        {
            if (saveEnabled.Equals("enable"))
            {
                if (saveToFile == false)
                {
                    saveFileName = FileName;
                    try
                    {
                        if (FileName == String.Empty) return false;
                        saveDataFile = new System.IO.StreamWriter(FileName, false);

                        System.Diagnostics.Debug.WriteLine("Starting to save data to file: " + FileName);
                        saveToFile = true;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("SaveToFile could not open file: " + FileName + ex.ToString());
                        return false;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Already writing to file.");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("No longer saving data to file.");
                saveToFile = false;
                if (saveDataFile != null)
                {
                    saveDataFile.Close();
                    saveDataFile = null;
                }
            }

            return true;
        }

        public void Kill()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("COM port kill.");                

                if (saveToFile == true)
                {
                    System.Diagnostics.Debug.WriteLine("No longer saving data to file.");
                    saveDataFile.Close();
                    saveDataFile = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("COM port already killed. Can't kill again: " + ex.ToString());
            }
        }

    }
}
