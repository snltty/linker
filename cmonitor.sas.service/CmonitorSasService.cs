using cmonitor.libs;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;

namespace cmonitor.sas.service
{
    partial class CmonitorSasService : ServiceBase
    {
        private readonly string[] args;
        public CmonitorSasService(string[] args)
        {
            this.args = args;
            InitializeComponent();
        }


        private string shareMkey = "cmonitor/share";
        private int shareMLength = 10;
        private int shareItemMLength = 1024;
        private int shareIndex = 4;
        private string mainArgs = string.Empty;
        private string mainExeName = "cmonitor";
        private byte[] keyBytes = Encoding.UTF8.GetBytes("cmonitor.sas.service");
        private ShareMemory shareMemory;

        private CancellationTokenSource cancellationTokenSource;

        protected override void OnStart(string[] _args)
        {
            try
            {
                if (args != null && args.Length > 0)
                {
                    shareMkey = args[0];
                    shareMLength = int.Parse(args[1]);
                    shareItemMLength = int.Parse(args[2]);
                    shareIndex = int.Parse(args[3]);
                    if (args.Length >= 5)
                    {
                        mainArgs = args[4];
                    }
                }

                shareMemory = new ShareMemory(shareMkey, shareMLength, shareItemMLength);
                shareMemory.InitGlobal();
                CheckMemory();
            }
            catch (Exception)
            {
            }
            CheckMainProcess();
        }
        protected override void OnStop()
        {
            WriteAllCloseState();
            WaitClose();
            cancellationTokenSource?.Cancel();
        }
        private void WaitClose()
        {
            while (Process.GetProcessesByName(mainExeName).Length != 0)
            {
                WriteAllCloseState();
                Thread.Sleep(1000);
            }
        }

        private void CheckMemory()
        {
            cancellationTokenSource = new CancellationTokenSource();
            Task.Factory.StartNew(async (a) =>
            {
                CancellationTokenSource tks = a as CancellationTokenSource;
                while (tks.IsCancellationRequested == false)
                {
                    try
                    {
                        string value = shareMemory.ReadValueString(shareIndex);
                        if (value == "ctrl+alt+delete")
                        {
                            try
                            {
                                SendSAS(false);
                            }
                            catch (Exception)
                            {
                            }
                            shareMemory.Update(shareIndex, keyBytes, Array.Empty<byte>());
                        }
                    }
                    finally
                    {
                    }

                    await Task.Delay(10);
                }
            }, cancellationTokenSource, TaskCreationOptions.LongRunning);
        }

        private void WriteAllCloseState()
        {
            for (int i = 0; i < shareMLength; i++)
            {
                shareMemory.AddAttribute(i, ShareMemoryAttribute.Closed);
            }
        }

        private Process proc;
        private void CheckMainProcess()
        {
            if (string.IsNullOrWhiteSpace(mainArgs))
            {
                return;
            }
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    try
                    {
                        if (Process.GetProcessesByName(mainExeName).Any() == false)
                        {
                            KillExe();
                            OpenExe();
                        }
                    }
                    catch (Exception)
                    {
                    }
                    await Task.Delay(30000);
                }
            }, TaskCreationOptions.LongRunning);
        }
        private bool OpenExe()
        {
            try
            {
                string filename = Process.GetCurrentProcess().MainModule.FileName;
                string dir = Path.GetDirectoryName(filename);
                string file = Path.Combine(dir, $"{mainExeName}.exe");
                ProcessStartInfo processStartInfo = new ProcessStartInfo()
                {
                    WorkingDirectory = dir,
                    FileName = file,
                    CreateNoWindow = false,
                    ErrorDialog = false,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    Arguments = string.Join(" ", this.args),
                    Verb = "runas",
                };
                proc = Process.Start(processStartInfo);

                return true;
            }
            catch (Exception)
            {
                try
                {
                    proc.Kill();
                    proc.Dispose();
                }
                catch (Exception)
                {
                }
                proc = null;
            }
            return false;
        }
        private void KillExe()
        {
            try
            {
                proc?.Close();
                proc?.Dispose();

            }
            catch (Exception)
            {
            }
            finally
            {
                proc = null;
            }
        }


        [DllImport("sas.dll")]
        public static extern void SendSAS(bool asUser);
    }
}
