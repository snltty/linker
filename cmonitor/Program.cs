using cmonitor.server.api;
using cmonitor.server.api.services;
using cmonitor.server.client;
using cmonitor.server.client.reports.active;
using cmonitor.server.client.reports.light;
using cmonitor.server.client.reports.hijack;
using cmonitor.server.client.reports.llock;
using cmonitor.server.client.reports.screen;
using cmonitor.server.client.reports.volume;
using cmonitor.server.client.reports.notify;
using cmonitor.server.client.reports.command;
using cmonitor.server.client.reports;
using cmonitor.server.client.reports.share;
using cmonitor.server.client.reports.system;
using cmonitor.server.service;
using cmonitor.server.service.messengers.active;
using cmonitor.server.service.messengers.hijack;
using cmonitor.server.service.messengers.llock;
using cmonitor.server.service.messengers.report;
using cmonitor.server.service.messengers.screen;
using cmonitor.server.service.messengers.sign;
using cmonitor.server.service.messengers.volume;
using cmonitor.server.service.messengers.wallpaper;
using cmonitor.server.service.messengers.keyboard;
using cmonitor.server.service.messengers.system;
using cmonitor.server.service.messengers.light;
using cmonitor.server.service.messengers.share;
using cmonitor.server.service.messengers.notify;
using cmonitor.server.service.messengers.setting;
using cmonitor.server.web;
using common.libs;
using common.libs.database;
using common.libs.extends;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Reflection;
using System.Text.Json.Serialization;
using cmonitor.server.client.reports.keyboard;
using cmonitor.server.client.reports.wallpaper;
using common.libs.winapis;


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

            //日志输出
            LoggerConsole();

            //读取参数
            Dictionary<string, string> dic = ArgumentParser.Parse(args, out string error);
#if RELEASE
            //提权
            if (dic.ContainsKey("elevated") == false)
            {
                Win32Interop.RelaunchElevated();
            }
#endif
            //初始化配置文件
            Config config = new Config();
            InitConfig(config, dic);


            //全局异常
            AppDomain.CurrentDomain.UnhandledException += (a, b) =>
            {
                Logger.Instance.Error(b.ExceptionObject + "");
            };
            //线程数
            ThreadPool.SetMinThreads(1024, 1024);
            ThreadPool.SetMaxThreads(65535, 65535);


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

            //客户端
            serviceCollection.AddSingleton<ClientSignInState>();
            serviceCollection.AddSingleton<ClientTransfer>();
            serviceCollection.AddSingleton<ClientConfig>();

            serviceCollection.AddSingleton<ReportTransfer>();

            serviceCollection.AddSingleton<ActiveWindowReport>();
            if (OperatingSystem.IsWindows()) serviceCollection.AddSingleton<IActiveWindow, ActiveWindowWindows>();
            else if (OperatingSystem.IsLinux()) serviceCollection.AddSingleton<IActiveWindow, ActiveWindowLinux>();
            else if (OperatingSystem.IsMacOS()) serviceCollection.AddSingleton<IActiveWindow, ActiveWindowMacOS>();

            serviceCollection.AddSingleton<HijackConfig>();
            serviceCollection.AddSingleton<HijackReport>();
            if (OperatingSystem.IsWindows()) serviceCollection.AddSingleton<IHijack, HijackWindows>();
            else if (OperatingSystem.IsLinux()) serviceCollection.AddSingleton<IHijack, HijackLinux>();
            else if (OperatingSystem.IsMacOS()) serviceCollection.AddSingleton<IHijack, HijackMacOS>();

            serviceCollection.AddSingleton<KeyboardReport>();
            if (OperatingSystem.IsWindows()) serviceCollection.AddSingleton<IKeyboard, KeyboardWindows>();
            else if (OperatingSystem.IsLinux()) serviceCollection.AddSingleton<IKeyboard, KeyboardLinux>();
            else if (OperatingSystem.IsMacOS()) serviceCollection.AddSingleton<IKeyboard, KeyboardMacOS>();

            serviceCollection.AddSingleton<LightReport>();
            if (OperatingSystem.IsWindows()) serviceCollection.AddSingleton<ILight, LightWindows>();
            else if (OperatingSystem.IsLinux()) serviceCollection.AddSingleton<ILight, LightLinux>();
            else if (OperatingSystem.IsMacOS()) serviceCollection.AddSingleton<ILight, LightMacOS>();

            serviceCollection.AddSingleton<LLockReport>();
            if (OperatingSystem.IsWindows()) serviceCollection.AddSingleton<ILLock, LLockWindows>();
            else if (OperatingSystem.IsLinux()) serviceCollection.AddSingleton<ILLock, LLockLinux>();
            else if (OperatingSystem.IsMacOS()) serviceCollection.AddSingleton<ILLock, LLockMacOS>();

            serviceCollection.AddSingleton<NotifyReport>();
            if (OperatingSystem.IsWindows()) serviceCollection.AddSingleton<INotify, NotifyWindows>();
            else if (OperatingSystem.IsLinux()) serviceCollection.AddSingleton<INotify, NotifyLinux>();
            else if (OperatingSystem.IsMacOS()) serviceCollection.AddSingleton<INotify, NotifyMacOS>();

            serviceCollection.AddSingleton<ScreenReport>();
            if (OperatingSystem.IsWindows()) serviceCollection.AddSingleton<IScreen, ScreenWindows>();
            else if (OperatingSystem.IsLinux()) serviceCollection.AddSingleton<IScreen, ScreenLinux>();
            else if (OperatingSystem.IsMacOS()) serviceCollection.AddSingleton<IScreen, ScreenMacOS>();

            serviceCollection.AddSingleton<VolumeReport>();
            if (OperatingSystem.IsWindows()) serviceCollection.AddSingleton<IVolume, VolumeWindows>();
            else if (OperatingSystem.IsLinux()) serviceCollection.AddSingleton<IVolume, VolumeLinux>();
            else if (OperatingSystem.IsMacOS()) serviceCollection.AddSingleton<IVolume, VolumeMacOS>();

            serviceCollection.AddSingleton<WallpaperReport>();
            if (OperatingSystem.IsWindows()) serviceCollection.AddSingleton<IWallpaper, WallpaperWindows>();
            else if (OperatingSystem.IsLinux()) serviceCollection.AddSingleton<IWallpaper, WallpaperLinux>();
            else if (OperatingSystem.IsMacOS()) serviceCollection.AddSingleton<IWallpaper, WallpaperMacOS>();

            serviceCollection.AddSingleton<SystemReport>();
            if (OperatingSystem.IsWindows()) serviceCollection.AddSingleton<ISystem, SystemWindows>();
            else if (OperatingSystem.IsLinux()) serviceCollection.AddSingleton<ISystem, SystemLinux>();
            else if (OperatingSystem.IsMacOS()) serviceCollection.AddSingleton<ISystem, SystemMacOS>();

            serviceCollection.AddSingleton<ShareReport>();
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
            serviceCollection.AddSingleton<VolumeMessenger>();
            serviceCollection.AddSingleton<WallpaperMessenger>();
            serviceCollection.AddSingleton<LightMessenger>();
            serviceCollection.AddSingleton<ShareMessenger>();
            serviceCollection.AddSingleton<NotifyMessenger>();
            serviceCollection.AddSingleton<SettingMessenger>();
            serviceCollection.AddSingleton<KeyboardMessenger>();
            serviceCollection.AddSingleton<SystemMessenger>();

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
            serviceCollection.AddSingleton<VolumeClientService>();
            serviceCollection.AddSingleton<WallpaperClientService>();
            serviceCollection.AddSingleton<LightClientService>();
            serviceCollection.AddSingleton<ShareClientService>();
            serviceCollection.AddSingleton<NotifyClientService>();
            serviceCollection.AddSingleton<SettingClientService>();
            serviceCollection.AddSingleton<SystemClientService>();
            serviceCollection.AddSingleton<KeyboardClientService>();


            //web
            serviceCollection.AddSingleton<IWebServer, WebServer>();
        }

        private static void InitConfig(Config config, Dictionary<string, string> dic)
        {
            try
            {
                config.Server = IPAddress.Parse(dic["server"]);
                config.Name = dic["name"];
                config.WebPort = int.Parse(dic["web"]);
                config.ApiPort = int.Parse(dic["api"]);
                config.ServicePort = int.Parse(dic["service"]);
                config.ShareMemoryKey = dic["share-key"];
                config.ShareMemoryLength = int.Parse(dic["share-len"]);
                config.ShareMemoryItemSize = int.Parse(dic["share-item-len"]);
                config.ReportDelay = int.Parse(dic["report-delay"]);
                config.ScreenScale = float.Parse(dic["screen-scale"]);
                config.ScreenDelay = int.Parse(dic["screen-delay"]);
                config.Elevated = dic.ContainsKey("elevated");

                Logger.Instance.Debug($"config:{config.ToJson()}");
                //Logger.Instance.Debug($"args:{string.Join(" ", args)}");

                config.IsCLient = dic.ContainsKey("mode") && dic["mode"].Contains("client");
                config.IsServer = dic.ContainsKey("mode") && dic["mode"].Contains("server");
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
        }

        private static void LoggerConsole()
        {
            Logger.Instance.LoggerLevel = LoggerTypes.DEBUG;
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

        /// <summary>
        /// 0项保留给各个功能的状态信息，每个一个字节为状态信息，看ShareMemoryState
        /// </summary>
        public string ShareMemoryKey { get; set; } = "cmonitor/share";
        public int ShareMemoryLength { get; set; } = 10;
        public int ShareMemoryItemSize { get; set; } = 1024;


        [JsonIgnore]
        public bool SaveSetting { get; set; } = true;
        [JsonIgnore]
        public bool WakeUp { get; set; } = true;
        [JsonIgnore]
        public bool VolumeMasterPeak { get; set; } = false;


        [JsonIgnore]
        public string Version { get; set; } = "1.0.0.1";
        [JsonIgnore]
        public bool IsCLient { get; set; }
        [JsonIgnore]
        public bool IsServer { get; set; }
        [JsonIgnore]
        public bool Elevated { get; set; }


        //键盘
        public const int ShareMemoryKeyBoardIndex = 1;
        //壁纸
        public const int ShareMemoryWallpaperIndex = 2;
        //锁屏
        public const int ShareMemoryLLockIndex = 3;
        //SAS
        public const int ShareMemorySASIndex = 4;

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
             && ValidateReport(dic, out error)
             && ValidateElevated(dic, out error);
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
                dic["share-len"] = "10";
            }
            if (dic.ContainsKey("share-item-len") == false || string.IsNullOrWhiteSpace(dic["share-item-len"]))
            {
                dic["share-item-len"] = "1024";
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
                dic["screen-delay"] = "200";
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

        static bool ValidateElevated(Dictionary<string, string> dic, out string error)
        {
            error = string.Empty;
            if (dic.ContainsKey("elevated"))
            {
                dic["elevated"] = "1";
            }
            return true;
        }

    }
}