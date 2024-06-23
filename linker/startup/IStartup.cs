using linker.config;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace linker.startup
{
    public interface IStartup
    {
        /// <summary>
        /// 插件名
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// 必须的
        /// </summary>
        public bool Required { get; }
        /// <summary>
        /// 加载顺序
        /// </summary>
        public StartupLevel Level { get; }
        /// <summary>
        /// 依赖哪些插件
        /// </summary>
        public string[] Dependent { get; }
        /// <summary>
        /// 加载方式
        /// </summary>
        public StartupLoadType LoadType { get; }

        public void AddClient(ServiceCollection serviceCollection, Config config, Assembly[] assemblies);
        public void UseClient(ServiceProvider serviceProvider, Config config, Assembly[] assemblies);

        public void AddServer(ServiceCollection serviceCollection, Config config, Assembly[] assemblies);
        public void UseServer(ServiceProvider serviceProvider, Config config, Assembly[] assemblies);
    }

    public enum StartupLoadType
    {
        /// <summary>
        /// 正常加载
        /// </summary>
        Normal = 0,
        /// <summary>
        /// 作为依赖，不主动加载，当被其它插件依赖时加载
        /// </summary>
        Dependent = 1,
    }

    public enum StartupLevel
    {
        Bottom = int.MinValue,

        Low9 = -9,
        Low8 = -8,
        Low7 = -7,
        Low6 = -6,
        Low5 = -5,
        Low4 = -4,
        Low3 = -3,
        Low2 = -2,
        Low1 = -1,
        Normal = 0,
        Hight1 = 1,
        Hight2 = 2,
        Hight3 = 3,
        Hight4 = 4,
        Hight5 = 5,
        Hight6 = 6,
        Hight7 = 7,
        Hight8 = 8,
        Hight9 = 9,

        Top = int.MaxValue
    }
}
