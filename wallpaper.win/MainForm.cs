using cmonitor.libs;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace wallpaper.win
{
    public partial class MainForm : Form
    {

        private IntPtr programIntPtr = IntPtr.Zero;
        private Hook hook;
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
            //this.WindowState = FormWindowState.Maximized;
            Find();
            Init();
            this.WindowState = FormWindowState.Maximized;

            shareMemory.InitLocal();

            WatchParent();
            WatchMemory();

            hook.Start();
        }

        private void Find()
        {
            // 通过类名查找一个窗口，返回窗口句柄。
            programIntPtr = Win32.FindWindow("Progman", null);
        }
        private void Init()
        {
            // 窗口句柄有效
            if (programIntPtr != IntPtr.Zero)
            {
                IntPtr result = IntPtr.Zero;
                // 向 Program Manager 窗口发送 0x52c 的一个消息，超时设置为0x3e8（1秒）。
                Win32.SendMessageTimeout(programIntPtr, 0x52c, IntPtr.Zero, IntPtr.Zero, 0, 0x3e8, result);

                // 遍历顶级窗口
                Win32.EnumWindows((hwnd, lParam) =>
                {
                    // 找到包含 SHELLDLL_DefView 这个窗口句柄的 WorkerW
                    if (Win32.FindWindowEx(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null) != IntPtr.Zero)
                    {
                        // 找到当前 WorkerW 窗口的，后一个 WorkerW 窗口。
                        IntPtr tempHwnd = Win32.FindWindowEx(IntPtr.Zero, hwnd, "WorkerW", null);

                        // 隐藏这个窗口
                        Win32.ShowWindow(tempHwnd, 0);
                    }
                    return true;
                }, IntPtr.Zero);

                // 窗口置父，设置背景窗口的父窗口为 Program Manager 窗口
                Win32.SetParent(this.Handle, programIntPtr);
            }
        }
        private void WatchParent()
        {
            IntPtr oldprogramIntPtr = programIntPtr;
            new Thread(() =>
            {
                while (cancellationTokenSource.Token.IsCancellationRequested == false)
                {
                    try
                    {
                        Find();
                        if (programIntPtr != oldprogramIntPtr)
                        {
                            cancellationTokenSource.Cancel();
                            Application.ExitThread();
                            Application.Exit();
                            Application.Restart();
                            Process.GetCurrentProcess().Kill();
                        }

                        bool hasChild = false;
                        Win32.EnumChildWindows(programIntPtr, (IntPtr hwnd, IntPtr lParam) =>
                        {
                            hasChild |= hwnd == this.Handle;
                            return true;
                        }, IntPtr.Zero);
                        if (hasChild == false)
                        {
                            Init();
                        }
                    }
                    catch (Exception)
                    {
                    }
                    Thread.Sleep(1000);
                }
            }).Start();
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
            shareMemory.WriteRunning(shareWallpaperIndex, false);
            shareMemory.WriteRunning(shareKeyBoardIndex, false);

            cancellationTokenSource.Cancel();
            hook.Close();

            Application.ExitThread();
            Application.Exit();
            Process.GetCurrentProcess().Kill();

            //shareMemory.Disponse();
        }

        byte[] keyBytes = Encoding.UTF8.GetBytes("KeyBoard");
        byte[] wallpaperBytes = Encoding.UTF8.GetBytes("Wallpaper");
        DateTime startTime = new DateTime(1970, 1, 1);
        byte[] emptyArray = new byte[0];
        private void WatchMemory()
        {
            shareMemory.WriteClosed(shareWallpaperIndex, false);
            shareMemory.WriteClosed(shareKeyBoardIndex, false);
            shareMemory.WriteRunning(shareWallpaperIndex, true);
            shareMemory.WriteRunning(shareKeyBoardIndex, true);
            new Thread(() =>
            {
                StringBuilder sb = new StringBuilder();
                while (cancellationTokenSource.Token.IsCancellationRequested == false)
                {
                    try
                    {
                        if (Hook.CurrentKeys == Keys.None)
                        {
                            shareMemory.Update(shareKeyBoardIndex, keyBytes, emptyArray);
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
                            sb.Append(Hook.CurrentKeys.ToString());

                            shareMemory.Update(shareKeyBoardIndex, keyBytes, Encoding.UTF8.GetBytes(sb.ToString()));
                        }
                        WriteWallpaper();
                    }
                    catch (Exception)
                    {
                    }

                    Thread.Sleep(30);
                }
            }).Start();
        }

        long lastTime = 0;
        private void WriteWallpaper()
        {
            long time = (long)(DateTime.UtcNow.Subtract(startTime)).TotalMilliseconds;
            if (time - lastTime >= 300)
            {
                shareMemory.Update(shareWallpaperIndex, wallpaperBytes, Encoding.UTF8.GetBytes(time.ToString()));
                if (shareMemory.ReadClosed(shareWallpaperIndex))
                {
                    CloseClear();
                }
                lastTime = time;
            }
        }
    }

    public static class Win32
    {
        [DllImport("user32.dll")]
        public static extern bool EnumChildWindows(IntPtr hWndParent, EnumChildCallback lpEnumFunc, IntPtr lParam);
        public delegate bool EnumChildCallback(IntPtr hwnd, IntPtr lParam);


        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string className, string winName);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessageTimeout(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam, uint fuFlage, uint timeout, IntPtr result);

        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsProc proc, IntPtr lParam);
        public delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string className, string winName);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hwnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern IntPtr SetParent(IntPtr hwnd, IntPtr parentHwnd);


        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);
        public const int SPI_SETDESKWALLPAPER = 20;
        public const int SPIF_UPDATEINIFILE = 0x01;
        public const int SPIF_SENDCHANGE = 0x02;
    }


    public class Hook : IDisposable
    {
        public delegate int HookProc(int nCode, int wParam, IntPtr lParam);
        static int hHook = 0;
        public const int WH_KEYBOARD_LL = 13;
        HookProc KeyBoardHookProcedure;
        [StructLayout(LayoutKind.Sequential)]
        public class KeyBoardHookStruct
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public int dwExtraInfo;
        }
        [DllImport("user32.dll")]
        public static extern int SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hInstance, int threadId);
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern bool UnhookWindowsHookEx(int idHook);
        [DllImport("user32.dll")]
        public static extern int CallNextHookEx(int idHook, int nCode, int wParam, IntPtr lParam);
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetModuleHandle(string name);
        public void Start()
        {
            // 安装键盘钩子 
            if (hHook == 0)
            {
                KeyBoardHookProcedure = new HookProc(KeyBoardHookProc);
                hHook = SetWindowsHookEx(WH_KEYBOARD_LL, KeyBoardHookProcedure, GetModuleHandle(null), 0);
                //如果设置钩子失败. 
                if (hHook == 0)
                    Close();
            }
            new Thread(() =>
            {
                while (true)
                {
                    if ((DateTime.Now - Hook.DateTime).TotalMilliseconds > 1000)
                    {
                        CurrentKeys = Keys.None;
                    }

                    Thread.Sleep(1000);
                }
            }).Start();
        }
        public void Close()
        {
            if (hHook != 0)
            {
                UnhookWindowsHookEx(hHook);
                hHook = 0;
            }
        }

        public static Keys CurrentKeys = Keys.None;
        public static DateTime DateTime = DateTime.Now;
        public static int KeyBoardHookProc(int nCode, int wParam, IntPtr lParam)
        {

            if (nCode >= 0)
            {
                KeyBoardHookStruct kbh = (KeyBoardHookStruct)Marshal.PtrToStructure(lParam, typeof(KeyBoardHookStruct));
                CurrentKeys = (Keys)kbh.vkCode;
                DateTime = DateTime.Now;

                /*
                if ((Control.ModifierKeys & Keys.LWin) == Keys.LWin && (Keys)kbh.vkCode == Keys.Tab)
                {
                    return 1;
                }
                */
            }
            return CallNextHookEx(hHook, nCode, wParam, lParam);
        }
        #region IDisposable 成员
        public void Dispose()
        {
            Close();
        }
        #endregion
    }
}
