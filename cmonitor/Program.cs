using common.libs;
using common.libs.database;
using common.libs.extends;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Reflection;
using System.Text.Json.Serialization;
using cmonitor.startup;
using cmonitor.config;

namespace cmonitor
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Init(args);

            //初始化配置文件
            Config config = new Config();
            config.Elevated = args.Any(c => c.Contains("elevated"));

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            StartupTransfer.Init();

            //依赖注入
            ServiceProvider serviceProvider = null;
            ServiceCollection serviceCollection = new ServiceCollection();
            //注入
            serviceCollection.AddSingleton((e) => serviceProvider);
            serviceCollection.AddSingleton((a) => config);
            serviceCollection.AddTransient(typeof(IConfigDataProvider<>), typeof(ConfigDataFileProvider<>));
            StartupTransfer.Add(serviceCollection, config, assemblies);

            //运行
            serviceProvider = serviceCollection.BuildServiceProvider();
            StartupTransfer.Use(serviceProvider, config, assemblies);


            GCHelper.FlushMemory();
            await Helper.Await();
        }

        private static void Init(string[] args)
        {
            //单服务
            Mutex mutex = new Mutex(true, System.Diagnostics.Process.GetCurrentProcess().ProcessName, out bool isAppRunning);
            if (isAppRunning == false)
            {
                Environment.Exit(1);
            }
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

#if RELEASE
            //提权
            if (args.Any(c=>c.Contains("elevated")) == false)
            {
                common.libs.winapis.Win32Interop.RelaunchElevated();
            }
#endif
        }

        private static void LoggerConsole()
        {
            Logger.Instance.LoggerLevel = LoggerTypes.DEBUG;
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