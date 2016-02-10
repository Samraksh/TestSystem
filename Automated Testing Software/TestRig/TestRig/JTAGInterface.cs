using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TestRig
{
    public class JTAGInterface
    {        
        public string[] textOCDInterface;
        public string[] textOCDInterfaceLocation;
        public string[] textOCDInterfaceGDBPort;
        public string[] textOCDInterfaceTelnetPort;

        public JTAGInterface(int numberInteraces)
        {
            textOCDInterface = new string[3];
            textOCDInterfaceLocation = new string[3];
            textOCDInterfaceGDBPort = new string[3];
            textOCDInterfaceTelnetPort = new string[3];

            textOCDInterface[0] = "Primary";
            for (int i = 1; i < numberInteraces; i++)
            {
                textOCDInterface[i] = "Support " + i.ToString();
            }

            for (int i = 0; i < numberInteraces; i++)
            {
                textOCDInterfaceGDBPort[i] = "3333";
                textOCDInterfaceTelnetPort[i] = "4444";
            }
        }

        public string getInterfaceName(int interfaceNumber)
        {
            return textOCDInterface[interfaceNumber];
        }

        public string getInterfaceLocation(int interfaceNumber)
        {
            return textOCDInterfaceLocation[interfaceNumber];
        }

        public bool setInterfaceLocation(int interfaceNumber, string location)
        {
            textOCDInterfaceLocation[interfaceNumber] = location;
            try
            {
                StreamReader reader;
                string line;
                reader = new StreamReader(location);

                line = reader.ReadLine();
                while (line != null)
                {
                    if (line.Contains("gdb_port") && (line[0] != '#'))
                    {
                        string[] parsedLine = line.Split(' ');
                        string port = parsedLine[1].Trim();
                        textOCDInterfaceGDBPort[interfaceNumber] = port;
                        System.Diagnostics.Debug.WriteLine(textOCDInterface[interfaceNumber] + " interface GDB port set to " + textOCDInterfaceGDBPort[interfaceNumber]);
                    }
                    if (line.Contains("telnet_port") && (line[0] != '#'))
                    {
                        string[] parsedLine = line.Split(' ');
                        string port = parsedLine[1].Trim();
                        textOCDInterfaceTelnetPort[interfaceNumber] = port;
                        System.Diagnostics.Debug.WriteLine(textOCDInterface[interfaceNumber] + " interface telnet port set to " + textOCDInterfaceTelnetPort[interfaceNumber]);
                    }
                    line = reader.ReadLine();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("setInterfaceLocation could not open file: " + location + ex.ToString());
                textOCDInterfaceGDBPort[interfaceNumber] = "3333";
                textOCDInterfaceTelnetPort[interfaceNumber] = "4444";
                return false;
            }

            return true;
        }

        public string getGDBPort(int interfaceNumber){
            return textOCDInterfaceGDBPort[interfaceNumber];
        }

        public string getTelnetPort(int interfaceNumber)
        {
            return textOCDInterfaceTelnetPort[interfaceNumber];
        }
    }
}
