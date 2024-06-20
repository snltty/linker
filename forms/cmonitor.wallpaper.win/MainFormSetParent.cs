using System.Diagnostics;
using System.Runtime.InteropServices;

namespace cmonitor.wallpaper.win
{
    internal sealed class MainFormSetParent
    {
        private IntPtr programIntPtr = IntPtr.Zero;
        private IntPtr mainformIntPtr = IntPtr.Zero;

        public void Start(IntPtr handle)
        {
            this.mainformIntPtr = handle;
            Find(); Init();
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
                Win32.SetParent(mainformIntPtr, programIntPtr);
            }
        }
        public void Watch(CancellationTokenSource cancellationTokenSource)
        {
            IntPtr oldprogramIntPtr = programIntPtr;
            Task.Run(async () =>
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
                            hasChild |= hwnd == mainformIntPtr;
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
                    await Task.Delay(1000);
                }
            });
        }
    }

    internal static class Win32
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
}
