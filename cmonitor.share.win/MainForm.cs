using cmonitor.libs;
using System.Diagnostics;

namespace cmonitor.share.win
{
    public partial class MainForm : Form
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

        private readonly Hook hook = new Hook();
        private readonly ShareMemory shareMemory;
        private readonly byte[] bytes;
        private long version = 0;
        public MainForm(string key, int size)
        {
            InitializeComponent();

            this.DoubleBuffered = true;

            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            UpdateStyles();

            bytes = new byte[size];
            shareMemory = new ShareMemory(key, 1, size);
            shareMemory.InitLocal();
        }

        private void OnLoad(object sender, EventArgs e)
        {
#if RELEASE
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.WindowState = FormWindowState.Maximized;
#endif
            TopMost = true;

            hook.Start();

            shareMemory.AddAttribute(0, ShareMemoryAttribute.Running);
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        if (shareMemory.ReadVersionUpdated(0, ref version))
                        {
                            this.Invoke(() =>
                            {
                                this.Invalidate();
                            });
                        }

                        if (shareMemory.ReadAttributeEqual(0, ShareMemoryAttribute.Closed))
                        {
                            shareMemory.RemoveAttribute(0, ShareMemoryAttribute.Running);
                            hook.Close();
                            Application.ExitThread();
                            Application.Exit();
                            Process.GetCurrentProcess().Kill();
                        }
                    }
                    catch (Exception)
                    {
                    }
                    await Task.Delay(15);
                }

            });
        }


        private void OnPaint(object sender, PaintEventArgs e)
        {
            int length = shareMemory.ReadValueArray(0, bytes);
            if (length > 0)
            {
                using MemoryStream stream = new MemoryStream(bytes, 0, length);
                using Bitmap bitmap = new Bitmap(stream);

                e.Graphics.Clear(Color.Black);
                e.Graphics.DrawImage(bitmap, 0, 0, this.ClientSize.Width, this.ClientSize.Height);
            }

            //base.OnPaint(e);
        }
    }
}
