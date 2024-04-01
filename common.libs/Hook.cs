using System;
using System.Runtime.InteropServices;

namespace common.libs
{
    public sealed class Hook
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

        private Func<int, bool> hookAction;
        public void Start(Func<int, bool> hookAction = null)
        {
            this.hookAction = hookAction;
            if (OperatingSystem.IsWindows() == false)
            {
                return;
            }
            // 安装键盘钩子 
            if (hHook != 0)
            {
                return;
            }
            KeyBoardHookProcedure = new HookProc(KeyBoardHookProc);
            hHook = SetWindowsHookEx(WH_KEYBOARD_LL, KeyBoardHookProcedure, GetModuleHandle(null), 0);
            //如果设置钩子失败. 
            if (hHook == 0)
            {
                Close();
                return;
            }


        }
        public void Close()
        {
            if (OperatingSystem.IsWindows() == false)
            {
                return;
            }
            if (hHook != 0)
            {
                UnhookWindowsHookEx(hHook);
                hHook = 0;
            }
        }

        public int CurrentKeys { get; set; }
        public DateTime LastDateTime { get; private set; } = DateTime.Now;
        private int KeyBoardHookProc(int nCode, int wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                KeyBoardHookStruct kbh = (KeyBoardHookStruct)Marshal.PtrToStructure(lParam, typeof(KeyBoardHookStruct));
                CurrentKeys = kbh.vkCode;
                LastDateTime = DateTime.Now;

                if (hookAction != null && hookAction.Invoke(nCode))
                {
                    return 1;
                }

            }
            return CallNextHookEx(hHook, nCode, wParam, lParam);
        }

    }
}
