// Credits goes to anandr_27
//  https://blogs.msdn.microsoft.com/padmanr/2010/05/08/execute-a-process-on-remote-machine-wait-for-it-to-exit-and-retrieve-its-exit-code-using-wmi/

using System;
using System.Management;
using System.Threading;

namespace RPLauncher
{
    public class ProcessWMI: IDisposable
    {
        public uint ProcessId;
        public int ExitCode;
        public bool EventArrived;
        public ManualResetEvent mre = new ManualResetEvent(false);
        public void ProcessStoptEventArrived(object sender, EventArrivedEventArgs e)
        {
            if ((uint)e.NewEvent.Properties["ProcessId"].Value == ProcessId)
            {
                //Console.WriteLine("Process: {0}, Stopped with Code: {1}", (int)(uint)e.NewEvent.Properties["ProcessId"].Value, (int)(uint)e.NewEvent.Properties["ExitStatus"].Value);
                ExitCode = (int)(uint)e.NewEvent.Properties["ExitStatus"].Value;
                EventArrived = true;
                mre.Set();
            }
        }
        public ProcessWMI()
        {
            this.ProcessId = 0;
            ExitCode = -1;
            EventArrived = false;
        }
        public void ExecuteRemoteProcessWMI(string remoteComputerName, string arguments, int WaitTimePerCommand)
        {
            string strUserName = string.Empty;
            try
            {
                ConnectionOptions connOptions = new ConnectionOptions
                {
                    Impersonation = ImpersonationLevel.Impersonate,
                    EnablePrivileges = true
                };

                ManagementScope manScope = new ManagementScope(String.Format(@"\\{0}\ROOT\CIMV2", remoteComputerName), connOptions);

                try
                {
                    manScope.Connect();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Management Connect to remote machine " + remoteComputerName + " as user " + strUserName + " failed with the following error " + e.Message);
                    Environment.ExitCode = 1;
                    return;
                }

                ObjectGetOptions objectGetOptions = new ObjectGetOptions();
                ManagementPath managementPath = new ManagementPath("Win32_Process");
                using (ManagementClass processClass = new ManagementClass(manScope, managementPath, objectGetOptions))
                {
                    using (ManagementBaseObject inParams = processClass.GetMethodParameters("Create"))
                    {
                        inParams["CommandLine"] = "cmd /c " + arguments;
                        using (ManagementBaseObject outParams = processClass.InvokeMethod("Create", inParams, null))
                        {
                            if ((uint)outParams["returnValue"] != 0)
                            {
                                Console.WriteLine("Error while starting command: " + arguments + "\r\nCreation returned an exit code: " + outParams["returnValue"] + "\r\nIt was launched as " + strUserName + "\r\nComputerName" + remoteComputerName);
                                Environment.ExitCode = 1;
                                return;
                            }
                            this.ProcessId = (uint)outParams["processId"];
                        }
                    }
                }

                SelectQuery CheckProcess = new SelectQuery("Select * from Win32_Process Where ProcessId = " + ProcessId);
                using (ManagementObjectSearcher ProcessSearcher = new ManagementObjectSearcher(manScope, CheckProcess))
                {
                    using (ManagementObjectCollection MoC = ProcessSearcher.Get())
                    {
                        if (MoC.Count == 0)
                        {
                            Console.WriteLine("ERROR AS WARNING: Process " + arguments + " terminated before it could be tracked on " + remoteComputerName);
                            Environment.ExitCode = 1;
                            return;
                        }
                    }
                }

                WqlEventQuery q = new WqlEventQuery("Win32_ProcessStopTrace");
                using (ManagementEventWatcher w = new ManagementEventWatcher(manScope, q))
                {
                    w.EventArrived += new EventArrivedEventHandler(this.ProcessStoptEventArrived);
                    w.Start();
                    if (!mre.WaitOne(WaitTimePerCommand, false))
                    {
                        w.Stop();
                        this.EventArrived = false;
                    }
                    else
                        w.Stop();
                }
                if (!this.EventArrived)
                {
                    SelectQuery sq = new SelectQuery("Select * from Win32_Process Where ProcessId = " + ProcessId);
                    using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(manScope, sq))
                    {
                        foreach (ManagementObject queryObj in searcher.Get())
                        {
                            queryObj.InvokeMethod("Terminate", null);
                            queryObj.Dispose();
                            Console.WriteLine("Process " + arguments + " timed out and was killed on " + remoteComputerName);
                            Environment.ExitCode = 1;
                            return;
                        }
                    }
                }
                else
                {
                    Environment.ExitCode = this.ExitCode;
                    return;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(string.Format("Execute process failed!\r\nMachinename:\t{0}\r\nCommand:\t{1}\r\nRunAs:\t{2}\r\nError:\t{3}\r\nStack trace:\r\n{4}", remoteComputerName, arguments, strUserName, e.Message, e.StackTrace), e);
                Environment.ExitCode = 1;
                return;
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    mre.Close();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~ProcessWMI() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
