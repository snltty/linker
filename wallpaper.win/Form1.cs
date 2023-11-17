using System;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace wallpaper.win
{
    public partial class Form1 : Form
    {

        private IntPtr programIntPtr = IntPtr.Zero;
        private Hook hook;
        private string imgUrl;
        private string shareMkey;
        private int shareMLength;
        private int shareItemMLength = 255;

        private int shareKeyBoardIndex;
        private int shareWallpaperIndex;

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
        public Form1(string imgUrl, string shareMkey, int shareMLength, int shareKeyBoardIndex, int shareWallpaperIndex)
        {
            this.imgUrl = imgUrl;
            this.shareMkey = shareMkey;
            this.shareMLength = shareMLength;
            this.shareKeyBoardIndex = shareKeyBoardIndex;
            this.shareWallpaperIndex = shareWallpaperIndex;


            InitializeComponent();

            hook = new Hook();
            AppDomain.CurrentDomain.ProcessExit += (s, e) => hook.Close();
            Application.ApplicationExit += (s, e) => hook.Close();

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

        MemoryMappedFile mmf2;
        MemoryMappedViewAccessor accessor2;
        byte[] keyBytes = Encoding.UTF8.GetBytes("KeyBoard");
        byte[] wallpaperBytes = Encoding.UTF8.GetBytes("Wallpaper");
        DateTime startTime = new DateTime(1970, 1, 1);
        byte[] emptyArray = new byte[0];

        private void OnLoad(object sender, EventArgs e)
        {
            pictureBox1.LoadCompleted += PictureBox1_LoadCompleted;
            pictureBox1.ImageLocation = imgUrl;

            this.Dock = DockStyle.Fill;
            this.ShowInTaskbar = false;
            this.FormBorderStyle = FormBorderStyle.None;

            hook.Start();

            Find();
            Init();

            this.WindowState = FormWindowState.Maximized;

            IntPtr oldprogramIntPtr = programIntPtr;
            new Thread(() =>
            {
                while (true)
                {
                    Find();
                    if (programIntPtr != oldprogramIntPtr)
                    {
                        Application.ExitThread();
                        Application.Exit();
                        Application.Restart();
                        Process.GetCurrentProcess().Kill();
                    }
                    Thread.Sleep(1000);
                }
            }).Start();


            mmf2 = MemoryMappedFile.CreateOrOpen($"{this.shareMkey}", this.shareMLength * shareItemMLength);
            accessor2 = mmf2.CreateViewAccessor();
            WriteKeyBoard("init");
            WriteMemory(this.shareWallpaperIndex, wallpaperBytes, Encoding.UTF8.GetBytes("init"));
            new Thread(() =>
            {
                StringBuilder sb = new StringBuilder();
                while (true)
                {
                    if (Hook.CurrentKeys == Keys.None)
                    {
                        ClearKeyBoard();
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

                        WriteKeyBoard(sb.ToString());
                    }
                    WriteWallpaper();

                    Thread.Sleep(30);
                }
            }).Start();
        }

        private void PictureBox1_LoadCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                pictureBox1.ImageLocation = "./bg.jpg";
            }
        }

        private void WriteKeyBoard(string value)
        {
            WriteMemory(this.shareKeyBoardIndex, keyBytes, Encoding.UTF8.GetBytes(value));
        }
        private void ClearKeyBoard()
        {
            WriteMemory(this.shareKeyBoardIndex, keyBytes, emptyArray);
        }

        long lastTime = 0;
        private void WriteWallpaper()
        {
            long time = (long)(DateTime.UtcNow.Subtract(startTime)).TotalMilliseconds;
            if (time - lastTime >= 300)
            {
                bool close = ReadCloseMemory(this.shareWallpaperIndex);
                WriteMemory(this.shareWallpaperIndex, wallpaperBytes, Encoding.UTF8.GetBytes(time.ToString()));
                if (close)
                {
                    Environment.Exit(0);
                }
                lastTime = time;
            }
        }
        private void WriteMemory(int index, byte[] key, byte[] value)
        {
            int keyIndex = index * shareItemMLength;
            if (value.Length > 0)
                accessor2.Write(keyIndex, (byte)key.Length);
            keyIndex++;
            if (value.Length > 0)
                accessor2.WriteArray(keyIndex, key, 0, key.Length);
            keyIndex += key.Length;

            accessor2.Write(keyIndex, (byte)value.Length);
            if (value.Length > 0)
            {
                keyIndex++;
                accessor2.WriteArray(keyIndex, value, 0, value.Length);
                keyIndex += value.Length;
            }
            UpdatedState(index);
        }
        private void UpdatedState(int updatedOffset)
        {
            accessor2.Write((shareMLength - 1) * shareItemMLength, (byte)1);
        }


        private bool ReadCloseMemory(int index)
        {
            int keyIndex = index * shareItemMLength;
            int keyLength = accessor2.ReadByte(keyIndex);
            keyIndex += 1 + keyLength;
            int valueLength = accessor2.ReadByte(keyIndex);
            keyIndex += 1;

            byte[] valueBytes = new byte[valueLength];
            if (valueBytes.Length > 0)
            {
                accessor2.ReadArray(keyIndex, valueBytes, 0, valueLength);
                string value = Encoding.UTF8.GetString(valueBytes, 0, valueLength);
                if (value == "close")
                {
                    return true;
                }
            }
            return false;
        }
    }

    public static class Win32
    {
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
