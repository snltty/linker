using linker.libs;
using linker.messenger.api;
using Microsoft.Extensions.DependencyInjection;
namespace linker.messenger.logger
{
    public static class Entry
    {
        public static ServiceCollection AddLoggerClient(this ServiceCollection serviceCollection)
        {
            LoggerConsole();
            serviceCollection.AddSingleton<LoggerApiController>();
            return serviceCollection;
        }
        public static ServiceProvider UseLoggerClient(this ServiceProvider serviceProvider)
        {
            IApiServer apiServer = serviceProvider.GetService<IApiServer>();
            apiServer.AddPlugins(new List<libs.api.IApiController> { serviceProvider.GetService<LoggerApiController>() });

            IAccessStore accessStore= serviceProvider.GetService<IAccessStore>();
            ILoggerStore loggerStore= serviceProvider.GetService<ILoggerStore>();
            if (accessStore.HasAccess(AccessValue.LoggerLevel) == false)
            {
                loggerStore.SetLevel( libs.LoggerTypes.WARNING);
                loggerStore.Confirm();
            }

            return serviceProvider;
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
