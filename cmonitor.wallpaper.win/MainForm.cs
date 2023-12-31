using cmonitor.libs;
using System.Diagnostics;
using System.Text;

namespace cmonitor.wallpaper.win
{
    public partial class MainForm : Form
    {


        private readonly Hook hook;
        private readonly MainFormSetParent mainFormSetParent;

        private string imgUrl;
        private int shareKeyBoardIndex;
        private int shareWallpaperIndex;
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

        public MainForm(string imgUrl, string shareMkey, int shareMLength, int shareItemMLength, int shareKeyBoardIndex, int shareWallpaperIndex)
        {
            this.imgUrl = imgUrl;
            this.shareKeyBoardIndex = shareKeyBoardIndex;
            this.shareWallpaperIndex = shareWallpaperIndex;

            shareMemory = new ShareMemory(shareMkey, shareMLength, shareItemMLength);

            InitializeComponent();

            hook = new Hook();
            mainFormSetParent = new MainFormSetParent();
            AppDomain.CurrentDomain.ProcessExit += (s, e) => CloseClear();
            Application.ApplicationExit += (s, e) => CloseClear();

        }


        private void OnLoad(object sender, EventArgs e)
        {
            pictureBox1.LoadCompleted += PictureBox1_LoadCompleted;
            pictureBox1.ImageLocation = imgUrl;

            this.Dock = DockStyle.Fill;
            this.ShowInTaskbar = false;
            this.FormBorderStyle = FormBorderStyle.None;

            Rectangle bound = Screen.PrimaryScreen.Bounds;
            this.Width = bound.Width;
            this.Height = bound.Height;
            this.Left = 0;
            this.Top = 0;

            mainFormSetParent.Start(this.Handle);
            this.WindowState = FormWindowState.Maximized;

            shareMemory.InitLocal();
            WatchMemory();


            mainFormSetParent.Watch(cancellationTokenSource);
            hook.Start();
        }


        private void PictureBox1_LoadCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                pictureBox1.ImageLocation = "./bg.jpg";
                try
                {
                    string filename = Process.GetCurrentProcess().MainModule.FileName;
                    string dir = Path.GetDirectoryName(filename);
                    string file = Path.Combine(dir, "bg.jpg");
                    Win32.SystemParametersInfo(Win32.SPI_SETDESKWALLPAPER, 0, file, Win32.SPIF_UPDATEINIFILE | Win32.SPIF_SENDCHANGE);

                }
                catch (Exception)
                {
                }
            }
        }
        private void CloseClear()
        {
            shareMemory.RemoveAttribute(shareWallpaperIndex, ShareMemoryAttribute.Running);
            shareMemory.RemoveAttribute(shareKeyBoardIndex, ShareMemoryAttribute.Running);

            shareMemory.Update(shareWallpaperIndex, wallpaperBytes, BitConverter.GetBytes((long)0));

            cancellationTokenSource.Cancel();
            hook.Close();

            Application.ExitThread();
            Application.Exit();
            Process.GetCurrentProcess().Kill();
        }

        private byte[] keyBytes = Encoding.UTF8.GetBytes("KeyBoard");
        private byte[] wallpaperBytes = Encoding.UTF8.GetBytes("Wallpaper");
        private DateTime startTime = new DateTime(1970, 1, 1);
        private byte[] emptyArray = new byte[0];
        private long lastTime = 0;

        private void WatchMemory()
        {
            shareMemory.RemoveAttribute(shareWallpaperIndex, ShareMemoryAttribute.Closed);
            shareMemory.RemoveAttribute(shareKeyBoardIndex, ShareMemoryAttribute.Closed);
            shareMemory.AddAttribute(shareWallpaperIndex, ShareMemoryAttribute.Running | ShareMemoryAttribute.HiddenForList);
            shareMemory.AddAttribute(shareKeyBoardIndex, ShareMemoryAttribute.Running);
            Task.Run(async () =>
            {
                StringBuilder sb = new StringBuilder();
                Keys lastKey = Keys.None;
                while (cancellationTokenSource.Token.IsCancellationRequested == false)
                {
                    try
                    {
                        if (hook.CurrentKeys == Keys.None)
                        {
                            if (lastKey != Keys.None)
                            {
                                shareMemory.Update(shareKeyBoardIndex, keyBytes, emptyArray);
                            }
                        }
                        else
                        {
                            sb.Clear();
                            if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
                            {
                                sb.Append("Ctrl+");
                            }
                            if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift)
                            {
                                sb.Append("Shift+");
                            }
                            if ((Control.ModifierKeys & Keys.Alt) == Keys.Alt)
                            {
                                sb.Append("Alt+");
                            }
                            sb.Append(hook.CurrentKeys.ToString());

                            shareMemory.Update(shareKeyBoardIndex, keyBytes, Encoding.UTF8.GetBytes(sb.ToString()));
                        }
                        lastKey = hook.CurrentKeys;
                        WriteWallpaper();
                    }
                    catch (Exception)
                    {
                    }

                    await Task.Delay(30);
                }
            });
        }
        private void WriteWallpaper()
        {
            long time = (long)(DateTime.UtcNow.Subtract(startTime)).TotalMilliseconds;
            if (time - lastTime >= 800)
            {
                shareMemory.Update(shareWallpaperIndex, wallpaperBytes, BitConverter.GetBytes(time));
                lastTime = time;
            }
            if (shareMemory.ReadAttributeEqual(shareWallpaperIndex, ShareMemoryAttribute.Closed))
            {
                CloseClear();
            }
        }
    }

}
