using linker.libs;
using System.ServiceProcess;
using System.Diagnostics;
using linker.messenger.entry;

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
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--config-client")
                {
                    configDic.Add("Client", args[i + 1]);
                    i++;
                }
                else if (args[i] == "--config-server")
                {
                    configDic.Add("Server", args[i + 1]);
                    i++;
                }
                else if (args[i] == "--config-action")
                {
                    configDic.Add("Action", args[i + 1]);
                    i++;
                }
                else if (args[i] == "--config-common")
                {
                    configDic.Add("Common", args[i + 1]);
                    i++;
                }
            }
            return configDic;
        }
    }

}