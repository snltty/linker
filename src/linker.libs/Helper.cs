using System;
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
        public static CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        public static void AppExit(int code)
        {
            OnAppExit?.Invoke(null, EventArgs.Empty);
            Environment.Exit(code);
        }
        public static async Task Await()
        {
            AppDomain.CurrentDomain.ProcessExit += (sender, e) => Exit();
            Console.CancelKeyPress += (sender, e) => Exit();
            try
            {
                await Task.Delay(-1, cancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch (Exception)
            {
            }
        }
        private static void Exit()
        {
            OnAppExit?.Invoke(null, EventArgs.Empty);
            cancellationTokenSource.Cancel();
        }
    }
}
