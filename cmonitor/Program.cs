using cmonitor.hijack;
using cmonitor.server.api;
using cmonitor.server.api.services;
using cmonitor.server.client;
using cmonitor.server.client.reports.active;
using cmonitor.server.client.reports.light;
using cmonitor.server.client.reports.hijack;
using cmonitor.server.client.reports.llock;
using cmonitor.server.client.reports.screen;
using cmonitor.server.client.reports.volume;
using cmonitor.server.service;
using cmonitor.server.service.messengers.active;
using cmonitor.server.service.messengers.hijack;
using cmonitor.server.service.messengers.llock;
using cmonitor.server.service.messengers.report;
using cmonitor.server.service.messengers.screen;
using cmonitor.server.service.messengers.sign;
using cmonitor.server.service.messengers.usb;
using cmonitor.server.service.messengers.volume;
using cmonitor.server.service.messengers.wallpaper;
using cmonitor.server.web;
using common.libs;
using common.libs.database;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using cmonitor.server.client.reports;
using common.libs.extends;
using cmonitor.server.service.messengers.light;
using System.Reflection;
using cmonitor.server.client.reports.share;
using cmonitor.server.service.messengers.share;
using cmonitor.server.client.reports.system;
using cmonitor.server.service.messengers.notify;
using cmonitor.server.client.reports.notify;
using cmonitor.server.client.reports.command;
using cmonitor.server.service.messengers.setting;

namespace cmonitor
{
    internal class Program
    {
        static async Task Main(string[] args)
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

            //初始化配置文件
            Config config = new Config();
            Dictionary<string, string> dic = ArgumentParser.Parse(args, out string error);
            InitConfig(config, dic);

            //注入对象
            ServiceProvider serviceProvider = null;
            ServiceCollection serviceCollection = new ServiceCollection();
            //注入 依赖注入服务供应 使得可以在别的地方通过注入的方式获得 ServiceProvider 以用来获取其它服务
            serviceCollection.AddSingleton((e) => serviceProvider);
            //注入
            serviceCollection.AddSingleton<Config>((a) => config);
            AddSingleton(serviceCollection);

            serviceProvider = serviceCollection.BuildServiceProvider();
            //运行服务
            RunService(serviceProvider, config);


            GCHelper.FlushMemory();
            await Helper.Await();
        }

        private static void RunService(ServiceProvider serviceProvider, Config config)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            MessengerResolver messengerResolver = serviceProvider.GetService<MessengerResolver>();
            messengerResolver.LoadMessenger(assemblies);

            if (config.IsServer)
            {
                Logger.Instance.Info($"start server");
                //api
                IClientServer clientServer = serviceProvider.GetService<IClientServer>();
                clientServer.LoadPlugins(assemblies);
                clientServer.Websocket();
                Logger.Instance.Info($"api listen:{config.ApiPort}");

                //web
                IWebServer webServer = serviceProvider.GetService<IWebServer>();
                webServer.Start();
                Logger.Instance.Info($"web listen:{config.WebPort}");

                //服务
                TcpServer tcpServer = serviceProvider.GetService<TcpServer>();
                tcpServer.Start();
                Logger.Instance.Info($"service listen:{config.ServicePort}");

            }
            if (config.IsCLient)
            {
                Logger.Instance.Info($"start client");
                Logger.Instance.Info($"server ip {config.Server}");

                ReportTransfer report = serviceProvider.GetService<ReportTransfer>();
                report.LoadPlugins(assemblies);

                ClientTransfer clientTransfer = serviceProvider.GetService<ClientTransfer>();
            }
        }

        private static void AddSingleton(ServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient(typeof(IConfigDataProvider<>), typeof(ConfigDataFileProvider<>));

            //劫持
            serviceCollection.AddSingleton<HijackConfig>();
            serviceCollection.AddSingleton<HijackController>();
            serviceCollection.AddSingleton<HijackEventHandler>();

            //客户端
            serviceCollection.AddSingleton<ClientSignInState>();
            serviceCollection.AddSingleton<ClientTransfer>();
            serviceCollection.AddSingleton<ClientConfig>();

            serviceCollection.AddSingleton<ReportTransfer>();
            serviceCollection.AddSingleton<ActiveWindowReport>();
            serviceCollection.AddSingleton<HijackReport>();
            serviceCollection.AddSingleton<LLockReport>();
            serviceCollection.AddSingleton<ScreenReport>();
            serviceCollection.AddSingleton<UsbReport>();
            serviceCollection.AddSingleton<VolumeReport>();
            serviceCollection.AddSingleton<WallpaperReport>();
            serviceCollection.AddSingleton<LightReport>();
            serviceCollection.AddSingleton<ShareReport>();
            serviceCollection.AddSingleton<SystemReport>();
            serviceCollection.AddSingleton<NotifyReport>();
            serviceCollection.AddSingleton<CommandReport>();


            //服务
            serviceCollection.AddSingleton<TcpServer>();
            serviceCollection.AddSingleton<MessengerSender>();
            serviceCollection.AddSingleton<MessengerResolver>();

            serviceCollection.AddSingleton<SignCaching>();
            serviceCollection.AddSingleton<SignInMessenger>();
            serviceCollection.AddSingleton<ReportMessenger>();
            serviceCollection.AddSingleton<CommandMessenger>();
            serviceCollection.AddSingleton<HijackMessenger>();
            serviceCollection.AddSingleton<ActiveMessenger>();
            serviceCollection.AddSingleton<LLockMessenger>();
            serviceCollection.AddSingleton<ScreenMessenger>();
            serviceCollection.AddSingleton<UsbMessenger>();
            serviceCollection.AddSingleton<VolumeMessenger>();
            serviceCollection.AddSingleton<WallpaperMessenger>();
            serviceCollection.AddSingleton<LightMessenger>();
            serviceCollection.AddSingleton<ShareMessenger>();
            serviceCollection.AddSingleton<NotifyMessenger>();
            serviceCollection.AddSingleton<SettingMessenger>();

            //api
            serviceCollection.AddSingleton<RuleConfig>();
            serviceCollection.AddSingleton<IClientServer, ClientServer>();
            serviceCollection.AddSingleton<SignInClientService>();
            serviceCollection.AddSingleton<CommandClientService>();
            serviceCollection.AddSingleton<ReportClientService>();
            serviceCollection.AddSingleton<HijackClientService>();
            serviceCollection.AddSingleton<ActiveClientService>();
            serviceCollection.AddSingleton<LLockClientService>();
            serviceCollection.AddSingleton<ScreenClientService>();
            serviceCollection.AddSingleton<UsbClientService>();
            serviceCollection.AddSingleton<VolumeClientService>();
            serviceCollection.AddSingleton<WallpaperClientService>();
            serviceCollection.AddSingleton<LightClientService>();
            serviceCollection.AddSingleton<ShareClientService>();
            serviceCollection.AddSingleton<NotifyClientService>();
            serviceCollection.AddSingleton<SettingClientService>();


            //web
            serviceCollection.AddSingleton<IWebServer, WebServer>();
        }
        private static void InitConfig(Config config, Dictionary<string, string> dic)
        {
            config.Server = IPAddress.Parse(dic["server"]);
            config.Name = dic["name"];
            config.WebPort = int.Parse(dic["web"]);
            config.ApiPort = int.Parse(dic["api"]);
            config.ServicePort = int.Parse(dic["service"]);
            config.ShareMemoryKey = dic["share-key"];
            config.ShareMemoryLength = int.Parse(dic["share-len"]);
            config.ReportDelay = int.Parse(dic["report-delay"]);
            config.ScreenScale = float.Parse(dic["screen-scale"]);
            config.ScreenDelay = int.Parse(dic["screen-delay"]);

            Logger.Instance.Debug($"config:{config.ToJson()}");
            //Logger.Instance.Debug($"args:{string.Join(" ", args)}");

            config.IsCLient = dic.ContainsKey("mode") && dic["mode"].Contains("client");
            config.IsServer = dic.ContainsKey("mode") && dic["mode"].Contains("server");
        }

        private static void LoggerConsole()
        {
            if (Directory.Exists("log") == false)
            {
                Directory.CreateDirectory("log");
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
                    using StreamWriter sw = File.AppendText(Path.Combine("log", $"{DateTime.Now:yyyy-MM-dd}.log"));
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

    public sealed class Config
    {
        public int WebPort { get; set; } = 1800;
        public int ApiPort { get; set; } = 1801;
        public int ServicePort { get; set; } = 1802;
        public IPAddress Server { get; set; } = IPAddress.Parse("192.168.1.18");
        public string WebRoot { get; set; } = "./web/";
        public string Name { get; set; } = Dns.GetHostName();

        public int ReportDelay { get; set; } = 30;

        public float ScreenScale { get; set; } = 0.2f;
        public int ScreenDelay { get; set; } = 30;

        public string Version { get; set; } = "1.0.0.1";
        public bool IsCLient { get; set; }
        public bool IsServer { get; set; }

        public string ShareMemoryKey { get; set; } = "cmonitor/share";
        public int ShareMemoryLength { get; set; } = ShareMemoryItemLength * 10;

        public const int ShareMemoryItemLength = 255;
        public const int ShareMemoryKeyBoardIndex = 0;
        public const int ShareMemoryWallpaperIndex = 1;
        public const int ShareMemoryLLockIndex = 2;

    }

    public class ArgumentParser
    {
        public static Dictionary<string, string> Parse(string[] args, out string error)
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].IndexOf("--") == 0)
                {
                    if (i + 1 < args.Length && args[i + 1].IndexOf("--") == -1)
                    {
                        dic.Add(args[i].Substring(2), args[i + 1]);
                        i++;
                    }
                    else
                    {
                        dic.Add(args[i].Substring(2), string.Empty);
                    }
                }
            }

            Validate(dic, out error);

            return dic;
        }
        static bool Validate(Dictionary<string, string> dic, out string error)
        {
            error = string.Empty;

            return ValidateMode(dic) &&
             ValidateServer(dic, out error)
             && ValidateName(dic, out error)
             && ValidatePort(dic, out error)
             && ValidateMemoryKey(dic, out error)
             && ValidateScreenScale(dic, out error)
             && ValidateReport(dic, out error);
        }
        static bool ValidateMode(Dictionary<string, string> dic)
        {
            //模式
            if (dic.ContainsKey("mode") == false || (dic["mode"].Contains("client") == false && dic["mode"].Contains("server") == false))
            {
                dic["mode"] = "server,client";
            }
            return true;
        }
        static bool ValidateServer(Dictionary<string, string> dic, out string error)
        {
            error = string.Empty;
            //服务器地址
            if (dic.ContainsKey("server") == false || string.IsNullOrWhiteSpace(dic["server"]))
            {
                dic["server"] = "192.168.1.35";
            }
            return true;
        }
        static bool ValidateName(Dictionary<string, string> dic, out string error)
        {
            error = string.Empty;
            //服务器地址
            if (dic.ContainsKey("name") == false || string.IsNullOrWhiteSpace(dic["name"]))
            {
                dic["name"] = Dns.GetHostName();
                if (dic["name"].Length > 12)
                {
                    dic["name"] = dic["name"].Substring(0, 12);
                }
            }
            return true;
        }

        static bool ValidatePort(Dictionary<string, string> dic, out string error)
        {
            error = string.Empty;
            //界面接口
            if (dic.ContainsKey("web") == false || string.IsNullOrWhiteSpace(dic["web"]))
            {
                dic["web"] = "1800";
            }
            //管理接口
            if (dic.ContainsKey("api") == false || string.IsNullOrWhiteSpace(dic["api"]))
            {
                dic["api"] = "1801";
            }
            //服务接口
            if (dic.ContainsKey("service") == false || string.IsNullOrWhiteSpace(dic["service"]))
            {
                dic["service"] = "1802";
            }
            return true;
        }

        static bool ValidateMemoryKey(Dictionary<string, string> dic, out string error)
        {
            error = string.Empty;
            if (dic.ContainsKey("share-key") == false || string.IsNullOrWhiteSpace(dic["share-key"]))
            {
                dic["share-key"] = "cmonitor/share";
            }
            if (dic.ContainsKey("share-len") == false || string.IsNullOrWhiteSpace(dic["share-len"]))
            {
                dic["share-len"] = "2550";
            }
            return true;
        }

        static bool ValidateScreenScale(Dictionary<string, string> dic, out string error)
        {
            error = string.Empty;
            if (dic.ContainsKey("screen-scale") == false || string.IsNullOrWhiteSpace(dic["screen-scale"]))
            {
                dic["screen-scale"] = "0.2";
            }
            if (dic.ContainsKey("screen-delay") == false || string.IsNullOrWhiteSpace(dic["screen-delay"]))
            {
                dic["screen-delay"] = "100";
            }
            return true;
        }
        static bool ValidateReport(Dictionary<string, string> dic, out string error)
        {
            error = string.Empty;
            if (dic.ContainsKey("report-delay") == false || string.IsNullOrWhiteSpace(dic["report-delay"]))
            {
                dic["report-delay"] = "30";
            }
            return true;
        }

    }
}