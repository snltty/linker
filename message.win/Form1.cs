using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace message.win
{
    public partial class Form1 : Form
    {
        private readonly int times = 10;
        public Form1(string msg,int times)
        {
            InitializeComponent();
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            label2.Text = msg;
            this.times = times;
        }


        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        public const uint SWP_NOMOVE = 0x2;
        public const uint SWP_NOSIZE = 0x1;
        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

        bool autoClose = false;
        private void Countdown()
        {
            new Thread(() =>
            {
                for (int i = times; i > 0; i--)
                {
                    this.Invoke(new EventHandler(delegate
                    {
                        label3.Text = i.ToString() + "s";
                    }));
                    System.Threading.Thread.Sleep(1000);
                }
                autoClose = true;
                this.Close();
            }).Start();
        }

        private void OnClosing(object sender, FormClosingEventArgs e)
        {
#if RELEASE
            if(autoClose == false)
            {
                e.Cancel = true;
            }
#endif
        }

        private void OnLoad(object sender, EventArgs e)
        {
            Countdown();

            //将窗口置顶
            SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
            //将窗口置底
            //SetWindowPos(this.Handle, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
        }
    }
}
