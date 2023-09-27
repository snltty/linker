using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace notify.win
{
    public partial class Form1 : Form
    {

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

        private int speed;
        private string msg;
        public Form1(int speed, string msg)
        {
            this.speed = speed;
            this.msg = msg;

            InitializeComponent();
            this.AllowTransparency = true;
            this.TransparencyKey = Color.Black;
            this.BackColor = Color.Black;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.TopMost = true;
            this.ShowInTaskbar = false;

            label1.BackColor = Color.FromArgb(255, 248, 153);
            label1.ForeColor = Color.Green;
        }

        private void OnLoad(object sender, EventArgs e)
        {
            this.FormBorderStyle = FormBorderStyle.None;
            //将窗口置顶
            SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);

            int style = GetWindowLong(this.Handle, GWL_STYLE);
            SetWindowLong(this.Handle, GWL_STYLE, style & ~WS_SYSMENU);

            // 添加窗体阴影
            SetWindowPos(this.Handle, IntPtr.Zero, 0, 0, 0, 0, SWP_FRAMECHANGED | SWP_NOSIZE | SWP_NOMOVE);


            RECT workAreaRect;
            SystemParametersInfo(SPI_GETWORKAREA, 0, out workAreaRect, 0);

            int screenWidth = workAreaRect.Right;
            int screenHeight = workAreaRect.Bottom;
            int width = this.Width;
            int height = this.Height;

            label1.Text = this.msg;

            Task.Run(() =>
            {
                this.Left = screenWidth + width;
                this.Top = screenHeight - height;
                while (true)
                {
                    this.Left -= this.speed;
                    if (this.Left < -width)
                    {
                        Application.Exit();
                    }

                    System.Threading.Thread.Sleep(10);
                }

            });
        }

        private const int SPI_GETWORKAREA = 0x0030;

        [DllImport("user32.dll")]
        private static extern bool SystemParametersInfo(int uAction, int uParam, out RECT lpRect, int fuWinIni);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }


        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        public const uint SWP_NOMOVE = 0x2;
        public const uint SWP_NOSIZE = 0x1;
        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        private const uint SWP_FRAMECHANGED = 0x20;
    }
}
