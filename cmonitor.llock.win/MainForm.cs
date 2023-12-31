using cmonitor.libs;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace cmonitor.llock.win
{
    public partial class MainForm : Form
    {
        private readonly Hook hook = new Hook();
        private int shareIndex;
        private readonly ShareMemory shareMemory;
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

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

        public MainForm(string shareMkey, int shareMLength, int shareItemMLength, int shareIndex)
        {
            this.shareIndex = shareIndex;
            shareMemory = new ShareMemory(shareMkey, shareMLength, shareItemMLength);
            shareMemory.InitLocal();

            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;

            AppDomain.CurrentDomain.ProcessExit += (s, e) => CloseClear();
            Application.ApplicationExit += (s, e) => CloseClear();

            btn1.Click += (s, e) => textBox1.Text += "1";
            btn2.Click += (s, e) => textBox1.Text += "2";
            btn3.Click += (s, e) => textBox1.Text += "3";
            btn4.Click += (s, e) => textBox1.Text += "4";
            btn5.Click += (s, e) => textBox1.Text += "5";
            btn6.Click += (s, e) => textBox1.Text += "6";
            btn7.Click += (s, e) => textBox1.Text += "7";
            btn8.Click += (s, e) => textBox1.Text += "8";
            btn9.Click += (s, e) => textBox1.Text += "9";
            btn0.Click += (s, e) => textBox1.Text += "0";
            btnClear.Click += (s, e) => textBox1.Text = "";

        }

        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        public const uint SWP_NOMOVE = 0x2;
        public const uint SWP_NOSIZE = 0x1;
        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

        private void OnLoad(object sender, EventArgs e)
        {
            hook.Start();
#if RELEASE
            this.WindowState = FormWindowState.Maximized;
#endif
            //将窗口置顶
            SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);

            groupBox1.Location = new System.Drawing.Point((this.Width - groupBox1.Width) / 2, (this.Height - groupBox1.Height) / 2);

            WriteLLock();
            shareMemory.RemoveAttribute(shareIndex, ShareMemoryAttribute.Closed);
            shareMemory.AddAttribute(shareIndex, ShareMemoryAttribute.Running | ShareMemoryAttribute.HiddenForList);
            Task.Run(async () =>
            {
                while (cancellationTokenSource.Token.IsCancellationRequested == false)
                {
                    if (shareMemory.ReadAttributeEqual(shareIndex, ShareMemoryAttribute.Closed))
                    {
                        CloseClear();
                    }
                    WriteLLock();
                    await Task.Delay(30);
                }
            });
        }
        private void OnClose(object sender, FormClosingEventArgs e)
        {
            CloseClear();
        }

        bool loading = false;
        private void button1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBox1.Text)) return;

            if (loading) return;
            loading = true;
            button1.Text = "ing.";

            try
            {
                DateTime dt = DateTime.Now;
                string psd = $"{dt.Hour / 10 % 10}{dt.Minute / 10 % 10}{dt.Hour % 10}{dt.Minute % 10}{dt.Month}{dt.Day}";
                if (psd == textBox1.Text)
                {
                    CloseClear();
                }
            }
            catch (Exception)
            {

            }
            loading = false;
            button1.Text = "解锁";
        }
        private void CloseClear()
        {
            shareMemory.RemoveAttribute(shareIndex, ShareMemoryAttribute.Running);
            shareMemory.Update(this.shareIndex, keyBytes, BitConverter.GetBytes((long)0));

            cancellationTokenSource.Cancel();
            hook.Close();

            Application.ExitThread();
            Application.Exit();
            Process.GetCurrentProcess().Kill();

            //shareMemory.Disponse();
        }

        private DateTime startTime = new DateTime(1970, 1, 1);
        private byte[] keyBytes = Encoding.UTF8.GetBytes("LLock");
        private long lastTime = 0;
        private void WriteLLock()
        {
            long time = (long)(DateTime.UtcNow.Subtract(startTime)).TotalMilliseconds;
            if (time - lastTime >= 800)
            {
                shareMemory.Update(this.shareIndex, keyBytes, BitConverter.GetBytes(time));
                lastTime = time;
            }
        }
    }



}
