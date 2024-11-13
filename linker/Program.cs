using linker.libs;
using Microsoft.Extensions.DependencyInjection;
using linker.startup;
using linker.config;
using System.ServiceProcess;
using System.Diagnostics;
using linker.libs.extends;

namespace linker
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            if (Environment.UserInteractive == false && OperatingSystem.IsWindows())
            {
                AppDomain.CurrentDomain.UnhandledException += (a, b) =>
                {
                };

                string serviceDirectory = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
                Directory.SetCurrentDirectory(serviceDirectory);

                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                    new Service()
                };
                ServiceBase.Run(ServicesToRun);
            }
            else
            {
                Run(args);
                await Helper.Await().ConfigureAwait(false);
            }

        }

        public static void Run(string[] args)
        {
            Init();

            //初始化配置文件
            FileConfig config = new FileConfig();
            //return;

            LoggerHelper.Instance.Warning($"current version : {config.Data.Version}");
            LoggerHelper.Instance.Warning($"linker env is docker : {Environment.GetEnvironmentVariable("SNLTTY_LINKER_IS_DOCKER")}");
            LoggerHelper.Instance.Warning($"linker env os : {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");

            StartupTransfer.Init(config);

            //依赖注入
            ServiceProvider serviceProvider = null;
            ServiceCollection serviceCollection = new ServiceCollection();
            //注入
            serviceCollection.AddSingleton((e) => serviceProvider);
            serviceCollection.AddSingleton((a) => config);
            StartupTransfer.Add(serviceCollection, config);

            //运行
            serviceProvider = serviceCollection.BuildServiceProvider();
            StartupTransfer.Use(serviceProvider, config);

            GCHelper.FlushMemory();
        }

        private static void Init()
        {
            //全局异常
            AppDomain.CurrentDomain.UnhandledException += (a, b) =>
            {
                LoggerHelper.Instance.Error(b.ExceptionObject + "");
            };
            //线程数
            ThreadPool.SetMinThreads(1024, 1024);
            ThreadPool.SetMaxThreads(65535, 65535);

            //日志输出
            LoggerConsole();
        }

        private static void LoggerConsole()
        {
            if (Directory.Exists("logs") == false)
            {
                Directory.CreateDirectory("logs");
            }
            LoggerHelper.Instance.OnLogger += (model) =>
            {
                ConsoleColor currentForeColor = Console.ForegroundColor;
                switch (model.Type)
                {
                    case LoggerTypes.DEBUG:
                        Console.ForegroundColor = ConsoleColor.Blue;
                        break;
                    case LoggerTypes.INFO:
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    case LoggerTypes.WARNING:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    case LoggerTypes.ERROR:
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                    default:
                        break;
                }
                string line = $"[{model.Type,-7}][{model.Time:yyyy-MM-dd HH:mm:ss}]:{model.Content}";
                Console.WriteLine(line);
                Console.ForegroundColor = currentForeColor;
                try
                {
                    using StreamWriter sw = File.AppendText(Path.Combine("logs", $"{DateTime.Now:yyyy-MM-dd}.log"));
                    sw.WriteLine(line);
                    sw.Flush();
                    sw.Close();
                    sw.Dispose();
                }
                catch (Exception)
                {
                }
            };
            TimerHelper.SetInterval(() =>
            {
                string[] files = Directory.GetFiles("logs").OrderBy(c => c).ToArray();
                for (int i = 0; i < files.Length - 180; i++)
                {
                    try
                    {
                        File.Delete(files[i]);
                    }
                    catch (Exception)
                    {
                    }
                }
                return true;
            }, 60 * 1000);
        }

    }

}