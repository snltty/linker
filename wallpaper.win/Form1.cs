using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace wallpaper.win
{
    public partial class Form1 : Form
    {

        private IntPtr programIntPtr = IntPtr.Zero;
        private Hook hook;
        private string img;
        private string key;
        private int len;

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
        public Form1(string img, string key,int len)
        {
            this.img = img;
            this.key = key;
            this.len = len;
            this.WindowState = FormWindowState.Maximized;
            this.FormBorderStyle = FormBorderStyle.None;
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

        private void OnLoad(object sender, EventArgs e)
        {
            pictureBox1.ImageLocation = img;
            this.Dock = DockStyle.Fill;
            ShowInTaskbar = false;

            hook.Start();

            Find();
            Init();

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


            MemoryMappedFile mmf2 = MemoryMappedFile.CreateOrOpen(this.key, this.len);
            MemoryMappedViewAccessor accessor2 = mmf2.CreateViewAccessor();
            new Thread(() =>
            {
                StringBuilder sb = new StringBuilder();
                while (true)
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

                    byte[] bytes = Encoding.UTF8.GetBytes(sb.ToString());
                    accessor2.Write(0, (byte)bytes.Length);
                    accessor2.WriteArray(1, bytes, 0, bytes.Length);

                    Thread.Sleep(30);
                }
            }).Start();
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
        public static int KeyBoardHookProc(int nCode, int wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                KeyBoardHookStruct kbh = (KeyBoardHookStruct)Marshal.PtrToStructure(lParam, typeof(KeyBoardHookStruct));
                CurrentKeys = (Keys)kbh.vkCode;
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
