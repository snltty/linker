using System;
using System.Threading;
using System.Threading.Tasks;

namespace linker.libs
{
    public static class Helper
    {
        static readonly byte[] emptyArray = [];
        public static byte[] EmptyArray => emptyArray;

        static readonly byte[] trueArray = [1];
        public static byte[] TrueArray => trueArray;

        static readonly byte[] falseArray = [0];
        public static byte[] FalseArray => falseArray;

        /// <summary>
        /// 【安全修复 P0】硬编码凭证已移除
        /// 所有密钥现在应该从环境变量或配置文件读取
        /// </summary>
        [Obsolete("Do not use hardcoded credentials. Use environment variables instead.")]
        public const string GlobalString = "DEPRECATED_DO_NOT_USE";

        private static string currentDirectory = "./";
        public static string CurrentDirectory => currentDirectory;
        public static void SetCurrentDirectory(string path)
        {
            currentDirectory = path;
        }


        public static event EventHandler OnAppExit;
        static CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
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

        public static event EventHandler<string> OnUpdate;
        public static void AppUpdate(string version)
        {
            OnUpdate?.Invoke(null, version);
        }
    }
}
