using common.libs;
using common.libs.database;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using cmonitor.startup;
using cmonitor.config;

namespace cmonitor
{
    internal class Program
    {
        /*
         * common.libs 是一些帮助，扩展，公共方法
         * cmonitor.tunnel 是打洞库
         * 
         */

        static async Task Main(string[] args)
        {
            Run(args);
            await Helper.Await();
        }

        public static void Run(string[] args)
        {
            Init();

            //初始化配置文件
            Config config = new Config();


            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            StartupTransfer.Init(config, assemblies);

            //依赖注入
            ServiceProvider serviceProvider = null;
            ServiceCollection serviceCollection = new ServiceCollection();
            //注入
            serviceCollection.AddSingleton((e) => serviceProvider);
            serviceCollection.AddSingleton((a) => config);
            StartupTransfer.Add(serviceCollection, config, assemblies);

            //运行
            serviceProvider = serviceCollection.BuildServiceProvider();
            StartupTransfer.Use(serviceProvider, config, assemblies);

            GCHelper.FlushMemory();
        }

        static Mutex mutex;
        private static void Init()
        {
            //单服务
#if RELEASEMONITOR
            mutex = new Mutex(true, System.Diagnostics.Process.GetCurrentProcess().ProcessName, out bool isAppRunning);
            if (isAppRunning == false)
            {
                Environment.Exit(1);
            }
#endif
            //全局异常
            AppDomain.CurrentDomain.UnhandledException += (a, b) =>
            {
                Logger.Instance.Error(b.ExceptionObject + "");
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
            Logger.Instance.OnLogger += (model) =>
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
        }

    }

}