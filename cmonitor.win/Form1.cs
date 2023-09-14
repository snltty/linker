using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace cmonitor.win
{
    public partial class Form1 : Form
    {
        private Process proc;
        private string[] args;
        protected override CreateParams CreateParams
        {
            get
            {
                const int WS_EX_APPWINDOW = 0x40000;
                const int WS_EX_TOOLWINDOW = 0x80;
                CreateParams cp = base.CreateParams;
                cp.ExStyle &= (~WS_EX_APPWINDOW);
                cp.ExStyle |= WS_EX_TOOLWINDOW;
                return cp;
            }
        }
        public Form1(string[] args)
        {
            this.args = args;
            InitializeComponent();

            this.FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            this.WindowState = FormWindowState.Minimized;
            this.Hide();
            this.Opacity = 0;

            AppDomain.CurrentDomain.ProcessExit += (s, e) => KillExe();
            Application.ApplicationExit += (s, e) => KillExe();


        }

        private bool OpenExe()
        {
            try
            {
                string filename = Process.GetCurrentProcess().MainModule.FileName;
                string dir = Path.GetDirectoryName(filename);
                string file = Path.Combine(dir, "./cmonitor.exe");
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

        private void OnLoad(object sender, EventArgs e)
        {
            OpenExe();
        }

        private void OnClose(object sender, FormClosingEventArgs e)
        {
            KillExe();
        }
    }
}
