using Microsoft.Win32;
using System;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace llock.win
{
    public partial class Form1 : Form
    {
        Hook hook = new Hook();
        private string shareMkey;
        private int shareMLength;
        private int shareIndex;
        

        public Form1(string shareMkey, int shareMLength, int shareIndex)
        {
            this.shareMkey = shareMkey;
            this.shareMLength = shareMLength;
            this.shareIndex = shareIndex;

            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;

            AppDomain.CurrentDomain.ProcessExit += (s, e) => hook.Close();
            Application.ApplicationExit += (s, e) => hook.Close();

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


            mmf2 = MemoryMappedFile.CreateOrOpen(this.shareMkey, this.shareMLength);
            accessor2 = mmf2.CreateViewAccessor();
            new Thread(() =>
            {
                while (true)
                {
                    WriteLLock();
                    Thread.Sleep(100);
                }
            }).Start();
        }
        MemoryMappedFile mmf2;
        MemoryMappedViewAccessor accessor2;
        DateTime startTime = new DateTime(1970, 1, 1);
        byte[] keyBytes = Encoding.UTF8.GetBytes("LLock");
        private void WriteLLock()
        {
            long time = (long)(DateTime.UtcNow.Subtract(startTime)).TotalMilliseconds;
            WriteMemory(this.shareIndex, keyBytes, Encoding.UTF8.GetBytes(time.ToString()));
        }
        private void WriteMemory(int index, byte[] key, byte[] value)
        {
            int keyIndex = index * 255;
            accessor2.Write(keyIndex, (byte)key.Length);
            keyIndex++;
            accessor2.WriteArray(keyIndex, key, 0, key.Length);
            keyIndex += key.Length;

            accessor2.Write(keyIndex, (byte)value.Length);
            keyIndex++;
            accessor2.WriteArray(keyIndex, value, 0, value.Length);
            keyIndex += value.Length;
        }

        private void OnClose(object sender, FormClosingEventArgs e)
        {
            hook.Close();
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
                string psd = $"{dt.Hour / 10 % 10}{dt.Minute / 10 % 10}{dt.Hour % 10}&{dt.Minute % 10}{dt.Month}{dt.Date}";
                if (psd == textBox1.Text)
                {
                    this.Close();
                }
            }
            catch (Exception)
            {
                
            }
            loading = false;
            button1.Text = "解锁";
        }


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
                else
                {
                    try
                    {
                        RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", true);
                        if (key == null)
                            key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System");
                        //任务管理器
                        key.SetValue("DisableTaskMgr", 1, RegistryValueKind.DWord);
                        //锁定
                        key.SetValue("DisableLockWorkstation", 1, RegistryValueKind.DWord);
                        //切换用户
                        key.SetValue("HideFastUserSwitching", 1, RegistryValueKind.DWord);
                        //修改密码
                        key.SetValue("DisableChangePassword", 1, RegistryValueKind.DWord);

                        key.Close();

                        //注销
                        RegistryKey zxKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", true);
                        if (zxKey == null)
                            zxKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer");
                        zxKey.SetValue("NoLogOff", 1, RegistryValueKind.DWord);
                        zxKey.Close();
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }
        public void Close()
        {
            bool retKeyboard = true;
            if (hHook != 0)
            {
                retKeyboard = UnhookWindowsHookEx(hHook);
                hHook = 0;
            }
            //如果去掉钩子失败. 
            //if (!retKeyboard) throw new Exception("UnhookWindowsHookEx failed.");
            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", true);
                if (key != null)
                {
                    key.DeleteValue("DisableTaskMgr", false);
                    key.DeleteValue("DisableLockWorkstation", false);
                    key.DeleteValue("HideFastUserSwitching", false);
                    key.DeleteValue("DisableChangePassword", false);

                    key.Close();
                }

                RegistryKey zxKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", true);
                if (zxKey != null)
                {
                    zxKey.DeleteValue("NoLogOff", false);
                    zxKey.Close();
                }
            }
            catch (Exception)
            {
            }
        }
        public static int KeyBoardHookProc(int nCode, int wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                return 1;
                KeyBoardHookStruct kbh = (KeyBoardHookStruct)Marshal.PtrToStructure(lParam, typeof(KeyBoardHookStruct));
                bool res = (kbh.vkCode >= (int)Keys.D0 && kbh.vkCode <= (int)Keys.D9)
                    || (kbh.vkCode >= (int)Keys.NumPad0 && kbh.vkCode <= (int)Keys.NumPad9)
                    || (kbh.vkCode >= (int)Keys.A && kbh.vkCode <= (int)Keys.Z);

                if (res == false)
                {
                    return 1;
                }
            }
            return CallNextHookEx(hHook, nCode, wParam, lParam);
        }
        #region IDisposable 成员
        public void Dispose()
        {
            Close();
        }

        public struct tagMSG
        {
            public int hwnd;
            public uint message;
            public int wParam;
            public long lParam;
            public uint time;
            public int pt;
        }
        [DllImport("user32.dll")]
        public static extern int GetMessage(ref tagMSG lpMsg, int a, int hwnd, int wMsgFilterMax);
        #endregion
    }
}
