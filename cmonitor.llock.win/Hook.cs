using common.libs;
using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace cmonitor.llock.win
{
    internal class Hook : IDisposable
    {
        private delegate int HookProc(int nCode, int wParam, IntPtr lParam);
        private static int hHook = 0;
        private const int WH_KEYBOARD_LL = 13;
        HookProc KeyBoardHookProcedure;
        [StructLayout(LayoutKind.Sequential)]
        private class KeyBoardHookStruct
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public int dwExtraInfo;
        }
        [DllImport("user32.dll")]
        private static extern int SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hInstance, int threadId);
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern bool UnhookWindowsHookEx(int idHook);
        [DllImport("user32.dll")]
        private static extern int CallNextHookEx(int idHook, int nCode, int wParam, IntPtr lParam);
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string name);

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
        private int KeyBoardHookProc(int nCode, int wParam, IntPtr lParam)
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

        private struct tagMSG
        {
            public int hwnd;
            public uint message;
            public int wParam;
            public long lParam;
            public uint time;
            public int pt;
        }
        [DllImport("user32.dll")]
        private static extern int GetMessage(ref tagMSG lpMsg, int a, int hwnd, int wMsgFilterMax);
        #endregion
    }
}
