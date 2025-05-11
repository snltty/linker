using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace linker.libs
{
    public static class Helper
    {
        public static byte[] EmptyArray = Array.Empty<byte>();
        public static byte[] TrueArray = new byte[] { 1 };
        public static byte[] FalseArray = new byte[] { 0 };

        public const string GlobalString = "snltty";

        public static string currentDirectory = "./";


        public static event EventHandler OnAppExit;
        public static void AppExit(int code)
        {
            OnAppExit?.Invoke(null, EventArgs.Empty);
            Environment.Exit(code);
        }
        public static async Task Await()
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            AppDomain.CurrentDomain.ProcessExit += (sender, e) => Exit(cancellationTokenSource);
            Console.CancelKeyPress += (sender, e) => Exit(cancellationTokenSource);
            await Task.Delay(-1, cancellationTokenSource.Token).ConfigureAwait(false);
        }
        private static void Exit(CancellationTokenSource cancellationTokenSource)
        {
            if(cancellationTokenSource.IsCancellationRequested == false)
            {
                cancellationTokenSource.Cancel();
                AppExit(0);
            }
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
