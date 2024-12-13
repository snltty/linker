using linker.config;
using linker.libs;
using Microsoft.Extensions.DependencyInjection;

namespace linker.startup
{
    public static partial class StartupTransfer
    {
        static List<IStartup> startups = new List<IStartup>();
        /// <summary>
        /// 反射读取所有插件
        /// </summary>
        /// <param name="config"></param>
        /// <param name="assemblies"></param>
        public static void Init(FileConfig config)
        {
            startups = GetSourceGeneratorInstances().OrderByDescending(c => c.Level).Distinct().ToList();
        }

        /// <summary>
        /// 注入
        /// </summary>
        /// <param name="serviceCollection"></param>
        /// <param name="config"></param>
        public static void Add(ServiceCollection serviceCollection, FileConfig config)
        {
            LoggerHelper.Instance.Info($"add startup : {string.Join(",", startups.Select(c => c.GetType().Name))}");
            foreach (var startup in startups)
            {
                if (config.Data.Common.Modes.Contains("client"))
                {
                    startup.AddClient(serviceCollection, config);
                }
                if (config.Data.Common.Modes.Contains("server"))
                {
                    startup.AddServer(serviceCollection, config);
                }
            }
        }

        /// <summary>
        /// 启动
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="config"></param>
        /// <param name="assemblies"></param>
        public static void Use(ServiceProvider serviceProvider, FileConfig config)
        {
            LoggerHelper.Instance.Info($"use startup : {string.Join(",", startups.Select(c => c.GetType().Name))}");
            foreach (var startup in startups)
            {
                if (config.Data.Common.Modes.Contains("client"))
                {
                    startup.UseClient(serviceProvider, config);
                }
                if (config.Data.Common.Modes.Contains("server"))
                {
                    startup.UseServer(serviceProvider, config);
                }
            }
        }
    }
}
