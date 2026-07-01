using linker.libs;
using linker.messenger.entry;
using linker.messenger.store.file;
using System.Diagnostics;
using System.Net;
using System.ServiceProcess;
using System.Text;
using System.Text.Json;

namespace linker
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
#if DEBUG
#else
            linker.libs.FireWallHelper.Write(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
#endif

            AppContext.SetSwitch("System.Net.Security.DisableServerNameChecks", true);
            ServicePointManager.CheckCertificateRevocationList = false;

            AppDomain.CurrentDomain.UnhandledException += (a, b) =>
            {
                LoggerHelper.Instance.Error(b.ExceptionObject + "");
            };
            /*
            TaskScheduler.UnobservedTaskException += (a, b) =>
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error(b.Exception + "");
            };
            */

            Directory.SetCurrentDirectory(AppContext.BaseDirectory);

            if (Environment.UserInteractive == false && OperatingSystem.IsWindows())
            {
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
            LinkerMessengerEntry.Initialize();
            LinkerMessengerEntry.Build();

            using JsonDocument json = ParseArgs(args);
            ConfigureByType(args);

            LinkerMessengerEntry.Setup(ExcludeModule.None, json);

            LoggerHelper.Instance.Warning($"current version : {VersionHelper.Version}");
            LoggerHelper.Instance.Warning($"linker env docker : {Environment.GetEnvironmentVariable("SNLTTY_LINKER_IS_DOCKER")}");
            LoggerHelper.Instance.Warning($"linker env os : {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
            LoggerHelper.Instance.Debug($"linker are running....");

            GCHelper.FlushMemory();
            GCHelper.EmptyWorkingSet();
        }

        private static void ConfigureByType(string[] args)
        {
            FileConfig config = LinkerMessengerEntry.GetService<FileConfig>();

            string type = Environment.GetEnvironmentVariable("SNLTTY_LINKER_MODE");
            if (string.IsNullOrEmpty(type) && args.Length == 1)
            {
                type = args[0];
            }

            switch (type)
            {
                case "client":
                    {
                        config.Data.Common.Modes = ["client"];
                    }
                    break;
                case "server":
                    {
                        ConfigServerInfo temp = new ConfigServerInfo();
                        config.Data.Common.Modes = ["server"];
                        config.Data.Server.ApiPort = temp.ApiPort;
                        config.Data.Server.SignIn.Anonymous = temp.SignIn.Anonymous;
                        config.Data.Server.SignIn.Enabled = temp.SignIn.Enabled;
                    }
                    break;
                case "node":
                    {
                        config.Data.Common.Modes = ["server"];
                        config.Data.Server.ApiPort = 0;
                        config.Data.Server.SignIn.Anonymous = false;
                        config.Data.Server.SignIn.Enabled = false;
                    }
                    break;
                default:
                    break;
            }
        }
        private static JsonDocument ParseArgs(string[] args)
        {

            JsonDocument json = null;
            try
            {
                json = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(args[0])));
            }
            catch (Exception ex)
            {
                if (args.Length == 1)
                    LoggerHelper.Instance.Error(args[0]);
                LoggerHelper.Instance.Warning($"args parse fail {ex}");
            }
            return json;
        }
    }

}