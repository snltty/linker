using cmonitor.libs;
using common.libs;
using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace llock.win
{
    public partial class MainForm : Form
    {
        Hook hook = new Hook();
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
            shareMemory.RemoveAttribute(shareIndex, ShareMemoryAttribute.Closed );
            shareMemory.AddAttribute(shareIndex,  ShareMemoryAttribute.Running | ShareMemoryAttribute.HiddenForList);
            new Thread(() =>
            {
                while (cancellationTokenSource.Token.IsCancellationRequested == false)
                {
                    if (shareMemory.ReadAttributeEqual(shareIndex, ShareMemoryAttribute.Closed))
                    {
                        CloseClear();
                    }
                    WriteLLock();
                    Thread.Sleep(30);
                }
            }).Start();
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

        DateTime startTime = new DateTime(1970, 1, 1);
        byte[] keyBytes = Encoding.UTF8.GetBytes("LLock");
        long lastTime = 0;
        private void WriteLLock()
        {
            long time = (long)(DateTime.UtcNow.Subtract(startTime)).TotalMilliseconds;
            if (time - lastTime >= 800)
            {
                shareMemory.Update(this.shareIndex, keyBytes,BitConverter.GetBytes(time));
                lastTime = time;
            }
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
                        foreach (string user in Registry.Users.GetSubKeyNames())
                        {
                            RegistryKey key = Registry.Users.OpenSubKey(user, true).OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", true);
                            if (key == null)
                                key = Registry.Users.OpenSubKey(user, true).CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System");

                            RegistryKey key1 = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", true);
                            if (key1 == null)
                                key1 = Registry.LocalMachine.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System");

                            //任务管理器
                            key.SetValue("DisableTaskMgr", 1, RegistryValueKind.DWord);
                            key1.SetValue("DisableTaskMgr", 1, RegistryValueKind.DWord);
                            //锁定
                            key.SetValue("DisableLockWorkstation", 1, RegistryValueKind.DWord);
                            key1.SetValue("DisableLockWorkstation", 1, RegistryValueKind.DWord);
                            //切换用户
                            key.SetValue("HideFastUserSwitching", 1, RegistryValueKind.DWord);
                            key1.SetValue("HideFastUserSwitching", 1, RegistryValueKind.DWord);
                            //修改密码
                            key.SetValue("DisableChangePassword", 1, RegistryValueKind.DWord);
                            key1.SetValue("DisableChangePassword", 1, RegistryValueKind.DWord);
                            //关机
                            key.SetValue("ShutdownWithoutLogon", 0, RegistryValueKind.DWord);
                            key1.SetValue("ShutdownWithoutLogon", 0, RegistryValueKind.DWord);
                            //注销
                            key.SetValue("StartMenuLogOff", 1, RegistryValueKind.DWord);
                            key1.SetValue("StartMenuLogOff", 1, RegistryValueKind.DWord);



                            key.Close();
                            key1.Close();

                            //注销
                            RegistryKey zxKey = Registry.Users.OpenSubKey(user, true).OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", true);
                            if (zxKey == null)
                                zxKey = Registry.Users.OpenSubKey(user, true).CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer");
                            zxKey.SetValue("NoLogOff", 1, RegistryValueKind.DWord);
                            zxKey.SetValue("NoClose", 1, RegistryValueKind.DWord);
                            zxKey.SetValue("StartMenuLogOff", 1, RegistryValueKind.DWord);
                            zxKey.Close();
                        }
                    }
                    catch (Exception)
                    {
                    }
                    Task.Run(() =>
                    {
                        CommandHelper.Windows(string.Empty, new string[] { "gpupdate /force" });
                    });
                }
            }
        }
        public void Close()
        {
            if (hHook != 0)
            {
                UnhookWindowsHookEx(hHook);
                hHook = 0;
            }
            try
            {
                foreach (string user in Registry.Users.GetSubKeyNames())
                {
                    RegistryKey key = Registry.Users.OpenSubKey(user, true).OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", true);
                    RegistryKey key1 = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", true);
                    if (key != null)
                    {
                        key.DeleteValue("DisableTaskMgr", false);
                        key1.DeleteValue("DisableTaskMgr", false);
                        key.DeleteValue("DisableLockWorkstation", false);
                        key1.DeleteValue("DisableLockWorkstation", false);
                        key.DeleteValue("HideFastUserSwitching", false);
                        key1.DeleteValue("HideFastUserSwitching", false);
                        key.DeleteValue("DisableChangePassword", false);
                        key1.DeleteValue("DisableChangePassword", false);
                        key.DeleteValue("ShutdownWithoutLogon", false);
                        key1.DeleteValue("ShutdownWithoutLogon", false);
                        key.DeleteValue("StartMenuLogoff", false);
                        key1.DeleteValue("StartMenuLogoff", false);


                        key.Close();
                        key1.Close();
                    }

                    RegistryKey zxKey = Registry.Users.OpenSubKey(user, true).OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", true);
                    if (zxKey != null)
                    {
                        zxKey.DeleteValue("NoLogOff", false);
                        zxKey.DeleteValue("NoClose", false);
                        zxKey.DeleteValue("StartMenuLogoff", false);
                        zxKey.Close();
                    }
                }
            }
            catch (Exception)
            {
            }
            CommandHelper.Windows(string.Empty, new string[] { "gpupdate /force" }, false);
        }
        public static int KeyBoardHookProc(int nCode, int wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                return 1;
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
