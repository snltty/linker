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

namespace cmonitor
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Mutex mutex = new Mutex(true, System.Diagnostics.Process.GetCurrentProcess().ProcessName, out bool isAppRunning);
            if (isAppRunning == false)
            {
                Environment.Exit(1);
            }

            AppDomain.CurrentDomain.UnhandledException += (a, b) =>
            {
                Logger.Instance.Error(b.ExceptionObject + "");
            };
            ThreadPool.SetMinThreads(1024, 1024);
            ThreadPool.SetMaxThreads(65535, 65535);
            LoggerConsole();



            Config config = new Config();
            Dictionary<string, string> dic = ArgumentParser.Parse(args, out string error);
            config.BroadcastIP = IPAddress.Parse(dic["server"]);
            config.Name = dic["name"];
            config.WebPort = int.Parse(dic["web"]);
            config.ApiPort = int.Parse(dic["api"]);
            config.ServicePort = int.Parse(dic["service"]);
            config.UserNameMemoryKey = dic["username-key"];
            config.UserNameMemoryLength = int.Parse(dic["username-len"]);
            config.KeyboardMemoryKey = dic["keyboard-key"];
            config.KeyboardMemoryLength = int.Parse(dic["keyboard-len"]);
            config.ShareMemoryKey = dic["share-key"];
            config.ShareMemoryLength = int.Parse(dic["share-len"]);
            Logger.Instance.Debug($"config:{config.ToJson()}");
            Logger.Instance.Debug($"args:{string.Join(" ", args)}");

            config.IsCLient = dic.ContainsKey("mode") && dic["mode"].Contains("client");
            config.IsServer = dic.ContainsKey("mode") && dic["mode"].Contains("server");
            //注入对象
            ServiceProvider serviceProvider = null;
            ServiceCollection serviceCollection = new ServiceCollection();
            //注入 依赖注入服务供应 使得可以在别的地方通过注入的方式获得 ServiceProvider 以用来获取其它服务
            serviceCollection.AddSingleton((e) => serviceProvider);

            serviceCollection.AddSingleton<Config>((a) => config);

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

            //web
            serviceCollection.AddSingleton<IWebServer, WebServer>();


            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            serviceProvider = serviceCollection.BuildServiceProvider();
            if (config.IsCLient || config.IsServer)
            {
                MessengerResolver messengerResolver = serviceProvider.GetService<MessengerResolver>();
                messengerResolver.LoadMessenger(assemblies);

            }
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
                Logger.Instance.Info($"server ip {config.BroadcastIP}");

                ReportTransfer report = serviceProvider.GetService<ReportTransfer>();
                report.LoadPlugins(assemblies);

                ClientTransfer clientTransfer = serviceProvider.GetService<ClientTransfer>();
            }

            GCHelper.FlushMemory();

            await Helper.Await();
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
        public IPAddress BroadcastIP { get; set; } = IPAddress.Parse("192.168.1.35");

        public bool IsCLient { get; set; }
        public bool IsServer { get; set; }

        public string WebRoot { get; set; } = "./web/";
        public string Name { get; set; } = Dns.GetHostName();

        public string Version { get; set; } = "1.0.0.1";


        public string UserNameMemoryKey { get; set; } = "cmonitor/username";
        public string KeyboardMemoryKey { get; set; } = "cmonitor/keyboard";

        public int UserNameMemoryLength { get; set; } = 255;
        public int KeyboardMemoryLength { get; set; } = 255;


        public string ShareMemoryKey { get; set; } = "cmonitor/sharememory";
        public int ShareMemoryLength { get; set; } = 1024;


        public const int ReportTime = 30;

        public const int ScreenTime = 200;


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
             ValidateServer(dic, out error) && ValidateName(dic, out error) && ValidatePort(dic, out error) && ValidateMemoryKey(dic, out error);
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
            if (dic.ContainsKey("username-key") == false || string.IsNullOrWhiteSpace(dic["username-key"]))
            {
                dic["username-key"] = "cmonitor/username";
            }
            if (dic.ContainsKey("username-len") == false || string.IsNullOrWhiteSpace(dic["username-len"]))
            {
                dic["username-len"] = "255";
            }

            if (dic.ContainsKey("keyboard-key") == false || string.IsNullOrWhiteSpace(dic["keyboard-key"]))
            {
                dic["keyboard-key"] = "cmonitor/keyboard";
            }
            if (dic.ContainsKey("keyboard-len") == false || string.IsNullOrWhiteSpace(dic["keyboard-len"]))
            {
                dic["keyboard-len"] = "255";
            }


            if (dic.ContainsKey("share-key") == false || string.IsNullOrWhiteSpace(dic["share-key"]))
            {
                dic["share-key"] = "cmonitor/share";
            }
            if (dic.ContainsKey("share-len") == false || string.IsNullOrWhiteSpace(dic["share-len"]))
            {
                dic["share-len"] = "1024";
            }
            return true;
        }

    }
}