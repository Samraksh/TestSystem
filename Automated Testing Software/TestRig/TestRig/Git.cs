using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace TestRig
{
    public class Git
    {
        public enum CommandStatus
        {
            Done,
            Running,
            Error
        }

        private CommandStatus commandResult;
        private string expectedResponse1, expectedResponse2;
        public StreamWriter input = null;

        public bool running;
        private string archive;
        private string matchedResponse;

        private StringWriter stdOutput = new StringWriter();
        public StringWriter Output { get { return stdOutput; } }
        private StringWriter stdError = new StringWriter();
        public StringWriter Error { get { return stdError; } }

        private static AutoResetEvent ARE_result = new AutoResetEvent(false);

        private Process GitProcess;
        public MainWindow mainHandle;

        public Git(MainWindow passedHandle)
        {
            mainHandle = passedHandle;  
            archive  = mainHandle.textTestSourcePath;            
            //archive = "d:\\Work\\TestSys\\Firstb";

            ProcessStartInfo openGitInfo = new ProcessStartInfo();
            GitProcess = new Process();

            System.Diagnostics.Debug.WriteLine("Starting Git.");

            openGitInfo.CreateNoWindow = true;
            openGitInfo.RedirectStandardOutput = true;
            openGitInfo.RedirectStandardInput = true;
            openGitInfo.UseShellExecute = false;
            openGitInfo.RedirectStandardError = true;
            openGitInfo.WorkingDirectory = mainHandle.textTestSourcePath;
            openGitInfo.FileName = @"cmd.exe";

            GitProcess.OutputDataReceived += new DataReceivedEventHandler(StandardOutputHandler);
            GitProcess.ErrorDataReceived += new DataReceivedEventHandler(StandardErrorHandler);

            GitProcess.StartInfo = openGitInfo;
            GitProcess.Start();
            input = GitProcess.StandardInput;
            GitProcess.BeginOutputReadLine();
            GitProcess.BeginErrorReadLine();
        }

        private bool ChangeDirectories(string directory)
        {
            // first change disks (might be needed)
            if (directory.Contains(':'))
            {
                int index = directory.IndexOf(':');
                string strippedName = directory.Substring(0, index + 1);

                // changing disks
                RunCommand(strippedName);
                // changing directory
                RunCommand(@"cd " + directory);
            }
            else
            {
                // no need to change disks, just directory
                RunCommand(@"cd " + directory);
            }

            return true;
        }

        public bool CloneCode()
        {            
            RunCommand(@"setlocal");                    
            RunCommand(@"set git_install_root=" + mainHandle.textGitPath);            
            RunCommand(@"set PATH=%git_install_root%\bin;%git_install_root%\mingw\bin;%git_install_root%\cmd;%PATH%");
            RunCommand(@"set PLINK_PROTOCOL=ssh");
            RunCommand(@"set HOME=%USERPROFILE%");            

            if (RunCommand(@"dir " + archive.ToString(), "File Not Found", "", 500) != CommandStatus.Done)
            {
                // test repository exists so we just checkout what we need to
                ChangeDirectories(archive.ToString());
                RunCommand(@"git reset --hard");
                if (RunCommand("git checkout master", "Switched to", "Already on", 500) != CommandStatus.Done)
                {
                    System.Diagnostics.Debug.WriteLine("Git failed to switch to branch.");
                    return false;
                }
                RunCommand(@"git clean -xdf");  
            }
            else
            {
                string userName = GetGitUserName();                
                int index = archive.LastIndexOf('\\') + 1;
                string archiveRoot = archive.Substring(index, archive.Length - index);
                // test repository does NOT exist so we clone it here                
                //if (RunCommand("git clone git@github.com:ChrisAtSamraksh/" + archive.ToString(), "Resolving deltas", String.Empty, 30000) != CommandStatus.Done)
                // For some reason the git responses are not redirected to our process so we have to use something else
                if (RunCommand("git clone git@github.com:" + userName + "/" + archiveRoot.ToString(), "Cloning into", String.Empty, 30000) != CommandStatus.Done)
                {
                    System.Diagnostics.Debug.WriteLine("Git failed to clone.");
                    return false;
                }
            }                                  
            return true;
        }

        public bool CloneCodeBranch(string textCodeBranch)
        {
            RunCommand(@"setlocal");
            RunCommand(@"set git_install_root=" + mainHandle.textGitPath + @"\Automated Testing Software\GitBin");
            RunCommand(@"set PATH=%git_install_root%\bin;%git_install_root%\mingw\bin;%git_install_root%\cmd;%PATH%");
            RunCommand(@"set PLINK_PROTOCOL=ssh");
            RunCommand(@"set HOME=%USERPROFILE%");

            if (RunCommand(@"dir " + archive.ToString(), "File Not Found", String.Empty, 500) != CommandStatus.Done)
            {
                // test repository exists so we just checkout what we need to
                ChangeDirectories(archive.ToString());
                RunCommand(@"git reset --hard");
                if (RunCommand("git checkout " + textCodeBranch, "Switched to", "Already on", 500) != CommandStatus.Done)
                {
                    System.Diagnostics.Debug.WriteLine("Git failed to switch to branch.");
                    return false;
                }
                RunCommand(@"git clean -xdf");
            }
            else
            {
                string userName = GetGitUserName();
                int index = archive.LastIndexOf('\\') + 1;
                string archiveRoot = archive.Substring(index, archive.Length - index);
                // test repository does NOT exist so we clone it here                
                //if (RunCommand("git clone git@github.com:ChrisAtSamraksh/" + archive.ToString() + " -b " + textCodeBranch, "Resolving deltas", String.Empty, 30000) != CommandStatus.Done)
                // For some reason the git responses are not redirected to our process so we have to use something else
                if (RunCommand("git clone git@github.com:" + userName + "/" + archiveRoot.ToString() + " -b " + textCodeBranch, "Cloning into", String.Empty, 30000) != CommandStatus.Done)
                {
                    System.Diagnostics.Debug.WriteLine("Git failed to clone.");
                    return false;
                }
            } 

            return true;
        }

        public string HeadRev()
        {
            RunCommand(@"setlocal");
            RunCommand(@"set git_install_root=" + mainHandle.textGitPath);
            RunCommand(@"set PATH=%git_install_root%\bin;%git_install_root%\mingw\bin;%git_install_root%\cmd;%PATH%");
            RunCommand(@"set PLINK_PROTOCOL=ssh");
            RunCommand(@"set HOME=%USERPROFILE%");


            if (RunCommand(@"dir " + archive.ToString(), "File Not Found", String.Empty, 500) != CommandStatus.Done)
            {
                ChangeDirectories(archive.ToString());
                RunCommand(@"git log --pretty=short -n 1", "commit", "NULL", 5000);
                string rev = matchedResponse.Split(' ')[1];
                return rev.Substring(0, 7);
            }
            else
            {
                return null;
            }
        }

        public string CurrentBranch()
        {
            RunCommand(@"setlocal");
            RunCommand(@"set git_install_root=" + mainHandle.textGitPath);
            RunCommand(@"set PATH=%git_install_root%\bin;%git_install_root%\mingw\bin;%git_install_root%\cmd;%PATH%");
            RunCommand(@"set PLINK_PROTOCOL=ssh");
            RunCommand(@"set HOME=%USERPROFILE%");


            if (RunCommand(@"dir " + archive.ToString(), "File Not Found", String.Empty, 500) != CommandStatus.Done)
            {
                ChangeDirectories(archive.ToString());
                RunCommand("git branch", "*", "NULL", 5000);

                string branch = matchedResponse.Split(' ')[1];
                return branch;
            }
            else
            {
                return null;
            }
            
        }

        public void Kill()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("GitProcess killed.");
                Utility.KillProcessAndChildren(GitProcess.Id);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("GitProcess already killed. Can't kill again: " + ex.ToString());
            }
        }

        private void ProcessResponse(string response)
        {
            if ((expectedResponse1 != String.Empty) && (expectedResponse1 != null))
            {
                if (response.Contains(expectedResponse1))
                {
                    commandResult = CommandStatus.Done;
                    matchedResponse = response;
                    ARE_result.Set();                    
                }
            }
            if ((expectedResponse2 != String.Empty) && (expectedResponse2 != null))
            {
                 if (response.Contains(expectedResponse2))
                 {
                     commandResult = CommandStatus.Done;
                     matchedResponse = response;
                     ARE_result.Set();                    
                 }
            }
        }

        private void StandardOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            System.Diagnostics.Debug.WriteLine("******************Git command result: " + outLine.Data);
            if (!String.IsNullOrEmpty(outLine.Data))
            {
                ProcessResponse(outLine.Data);
            }
        }

        private void StandardErrorHandler(object sendingProcess, DataReceivedEventArgs errLine)
        {
            System.Diagnostics.Debug.WriteLine("******************Git error: " + errLine.Data);
            if (!String.IsNullOrEmpty(errLine.Data))
            {
                ProcessResponse(errLine.Data);
            }
        }

        private bool RunCommand(string command)
        {
            System.Diagnostics.Debug.WriteLine("Git run command: " + command);
            input.WriteLine(command);

            return true;
        }

        private CommandStatus RunCommand(string command, string expect1, string expect2, int timeout)
        {
            int attempts;
            expectedResponse1 = expect1;
            expectedResponse2 = expect2;

            for (attempts = 0; attempts < 3; attempts++)
            {
                System.Diagnostics.Debug.WriteLine("Git run attempt " + attempts.ToString() + " for: " + command + " waiting for: " + expect1.ToString() + " or " + expect2.ToString());
                commandResult = CommandStatus.Running;
                input.WriteLine(command);
                ARE_result.WaitOne(timeout);
                if (commandResult == CommandStatus.Done)
                    return commandResult;
            }
            return commandResult;
        }

        private string GetGitUserName()
        {
            string userName;

            matchedResponse = String.Empty;
            RunCommand("git config --list", "user.name","NULL",5000);

            userName = matchedResponse.Split('=')[1];
            return userName;
        }
    }
}
