using linker.libs;
using System.ServiceProcess;
using System.Diagnostics;
using linker.messenger.entry;
using linker.libs.extends;

namespace linker
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
#if DEBUG
#else
            //添加防火墙，不添加ICMP
            linker.libs.FireWallHelper.Write(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
#endif
            //全局异常
            AppDomain.CurrentDomain.UnhandledException += (a, b) =>
            {
                LoggerHelper.Instance.Error(b.ExceptionObject + "");
            };

            //线程数
            //ThreadPool.SetMinThreads(1024, 1024);
            //ThreadPool.SetMaxThreads(65535, 65535);

            string serviceDirectory = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            Directory.SetCurrentDirectory(serviceDirectory);

            //windows服务运行
            if (Environment.UserInteractive == false && OperatingSystem.IsWindows())
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                    new Service()
                };
                ServiceBase.Run(ServicesToRun);
            }
            //正常运行
            else
            {
                Run(args);
                await Helper.Await().ConfigureAwait(false);
            }

        }

        public static void Run(string[] args)
        {
            Dictionary<string, string> configDic = ParseArgs(args);

            LinkerMessengerEntry.Initialize();
            LinkerMessengerEntry.Build();

            LinkerMessengerEntry.Setup(ExcludeModule.None, configDic);

            LoggerHelper.Instance.Warning($"current version : {VersionHelper.version}");
            LoggerHelper.Instance.Warning($"linker env is docker : {Environment.GetEnvironmentVariable("SNLTTY_LINKER_IS_DOCKER")}");
            LoggerHelper.Instance.Warning($"linker env os : {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
            LoggerHelper.Instance.Debug($"linker are running....");

            GCHelper.FlushMemory();
        }

        private static Dictionary<string, string> ParseArgs(string[] args)
        {
            Dictionary<string, string> configDic = new Dictionary<string, string>();
            try
            {
                configDic = args[0].DeJson<Dictionary<string, string>>();
            }
            catch (Exception)
            {
            }
            return configDic;
        }
    }

}