using System.Runtime.InteropServices;

namespace wallpaper.win
{
    internal sealed class Hook : IDisposable
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
            }
            Task.Run(async () =>
            {
                while (true)
                {
                    if ((DateTime.Now - lastDateTime).TotalMilliseconds > 1000)
                    {
                        CurrentKeys = Keys.None;
                    }

                    await Task.Delay(1000);
                }
            });
        }
        public void Close()
        {
            if (hHook != 0)
            {
                UnhookWindowsHookEx(hHook);
                hHook = 0;
            }
        }

        public Keys CurrentKeys { get; set; } = Keys.None;
        private DateTime lastDateTime = DateTime.Now;
        private int KeyBoardHookProc(int nCode, int wParam, IntPtr lParam)
        {

            if (nCode >= 0)
            {
                KeyBoardHookStruct kbh = (KeyBoardHookStruct)Marshal.PtrToStructure(lParam, typeof(KeyBoardHookStruct));
                CurrentKeys = (Keys)kbh.vkCode;
                lastDateTime = DateTime.Now;
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
