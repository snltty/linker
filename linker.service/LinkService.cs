using System.Diagnostics;
using System.ServiceProcess;

namespace linker.service
{
    partial class LinkService : ServiceBase
    {
        private readonly string[] args;
        public LinkService(string[] args)
        {
            this.args = args;
            InitializeComponent();
        }

        private string mainExeName = "linker";
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        protected override void OnStart(string[] _args)
        {
            CheckMainProcess();
        }
        protected override void OnStop()
        {
            cancellationTokenSource?.Cancel();
            KillExe();

        }

        private Process proc;
        private void CheckMainProcess()
        {
            Task.Run(async () =>
            {
                while (cancellationTokenSource.IsCancellationRequested == false)
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
                    await Task.Delay(3000);
                }
            });
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
                    //Arguments = string.Join(" ", this.args),
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

                foreach (var item in Process.GetProcessesByName(mainExeName))
                {
                    item.Kill();
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                proc = null;
            }
        }
    }
}
