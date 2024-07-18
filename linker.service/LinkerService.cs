using System.Diagnostics;
using System.ServiceProcess;

namespace linker.service
{
    partial class LinkerService : ServiceBase
    {
        private readonly string[] args;
        public LinkerService(string[] args)
        {
            this.args = args;
            InitializeComponent();
        }

        private string mainExeName = "linker";
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        protected override void OnStart(string[] _args)
        {
            OpenExe();
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
                            RestartService();
                        }
                    }
                    catch (Exception)
                    {
                    }
                    await Task.Delay(3000).ConfigureAwait(false);
                }
            });
        }
        private bool OpenExe()
        {
            try
            {
                string filename = Process.GetCurrentProcess().MainModule.FileName;
                string dir = Path.GetDirectoryName(filename);
                proc = Process.Start(new ProcessStartInfo()
                {
                    WorkingDirectory = dir,
                    FileName = Path.Combine(dir, $"{mainExeName}.exe"),
                    CreateNoWindow = false,
                    ErrorDialog = false,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    //Arguments = string.Join(" ", this.args),
                    Verb = "runas",
                });
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

        public void RestartService()
        {
            try
            {
                KillExe();
                Environment.Exit(1);
            }
            catch (Exception)
            {
            }
        }
    }
}
