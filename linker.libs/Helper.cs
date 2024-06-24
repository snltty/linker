using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Linker.Libs
{
    public static class Helper
    {
        public static byte[] EmptyArray = Array.Empty<byte>();
        public static byte[] TrueArray = new byte[] { 1 };
        public static byte[] FalseArray = new byte[] { 0 };

        public static string GlobalString = "snltty";

        public static async Task Await()
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            AppDomain.CurrentDomain.ProcessExit += (sender, e) => cancellationTokenSource.Cancel();
            Console.CancelKeyPress += (sender, e) => cancellationTokenSource.Cancel();
            await Task.Delay(-1, cancellationTokenSource.Token);
        }

        static DateTime startTime = new DateTime(1970, 1, 1);
        public static long Timestamp()
        {
            return (long)(DateTime.UtcNow.Subtract(startTime)).TotalMilliseconds;
        }


        private delegate bool ConsoleCtrlDelegate(int ctrlType);
        private static ConsoleCtrlDelegate handler;
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate handler, bool add);
        private const int CTRL_CLOSE_EVENT = 2;

        public static void BlockClose()
        {
            handler = new ConsoleCtrlDelegate(ConsoleCtrlHandler);
            SetConsoleCtrlHandler(handler, true);
        }
        private static bool ConsoleCtrlHandler(int ctrlType)
        {
            if (ctrlType == CTRL_CLOSE_EVENT)
            {
                // 阻止窗口关闭
                return true;
            }
            return false;
        }

    }
}
