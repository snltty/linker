using linker.messenger.api;
using linker.messenger.decenter;
using linker.messenger.exroute;
using linker.messenger.flow;
using linker.messenger.forward;
using linker.messenger.listen;
using linker.messenger.logger;
using Microsoft.Extensions.DependencyInjection;
using linker.messenger.pcp;
using linker.messenger.relay;
using linker.messenger.sforward;
using linker.messenger.signin;
using linker.messenger.socks5;
using linker.messenger.sync;
using linker.messenger.tunnel;
using linker.messenger.tuntap;
using linker.messenger.updater;
using linker.messenger.store.file;
using linker.messenger.serializer.memorypack;
using linker.libs;

namespace linker.messenger.entry
{
    public static class LinkerMessengerEntry
    {
        private static ServiceCollection serviceCollection;
        private static ServiceProvider serviceProvider;
        private static OperatingManager inited = new OperatingManager();
        private static OperatingManager builded = new OperatingManager();
        private static OperatingManager setuped = new OperatingManager();

        /// <summary>
        /// 开始初始化
        /// </summary>
        /// <returns></returns>
        public static void Initialize()
        {
            if (inited.StartOperation() == false) return;

            serviceCollection = new ServiceCollection();

            serviceCollection
                //日志
                .AddLoggerClient()
                //api接口和web
                .AddApiClient()
                //路由排除
                .AddExRoute()

                //服务器监听
                .AddListen()

                //权限
                .AddAccessClient().AddAccessServer()
                //自定义验证
                .AddActionClient().AddActionServer()
                //数据同步
                .AddDecenterClient().AddDecenterServer()
                //端口转发
                .AddForwardClient().AddForwardServer()
                //pcp
                .AddPcpClient().AddPcpServer()
                //中继
                .AddRelayClient().AddRelayServer()
                //服务器穿透
                .AddSForwardClient().AddSForwardServer()
                //登录
                .AddSignInClient().AddSignInServer()
                //socks5
                .AddSocks5Client().AddSocks5Server()
                //同步
                .AddSyncClient().AddSyncServer()
                //打洞
                .AddTunnelClient().AddTunnelServer()
                //虚拟网卡
                .AddTuntapClient().AddTuntapServer()
                //更新
                .AddUpdaterClient().AddUpdaterServer()

                //信标
                .AddMessenger()
                //流量统计
                .AddFlowClient().AddFlowServer()

                //持久化，文件
                .AddStoreFile()
                //序列化 MemoryPack
                .AddSerializerMemoryPack();
        }
        /// <summary>
        /// 注入
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        public static void AddService<TService>() where TService : class
        {
            serviceCollection.AddSingleton<TService>();
        }
        /// <summary>
        /// 注入
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        public static void AddService<TService, TImplementation>() where TService : class where TImplementation : class, TService
        {
            serviceCollection.AddSingleton<TService, TImplementation>();
        }

        /// <summary>
        /// 构建
        /// </summary>
        /// <returns></returns>
        public static void Build()
        {
            if (builded.StartOperation() == false) return;

            serviceProvider = serviceCollection.BuildServiceProvider();

        }
        /// <summary>
        /// 获取服务
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <returns></returns>
        public static TService GetService<TService>() where TService : class
        {
            return serviceProvider.GetService<TService>();
        }


        /// <summary>
        /// 开始运行
        /// </summary>
        /// <param name="modules">排除哪些模块，默认无</param>
        public static void Setup(ExcludeModule modules = ExcludeModule.None, Dictionary<string, string> configDic = null)
        {
            if (setuped.StartOperation() == false) return;

            ICommonStore commonStore = serviceProvider.GetService<ICommonStore>();

            serviceProvider.UseMessenger();
            if ((modules & ExcludeModule.StoreFile) != ExcludeModule.StoreFile)
                serviceProvider.UseStoreFile(configDic);
            if ((modules & ExcludeModule.SerializerMemoryPack) != ExcludeModule.SerializerMemoryPack)
                serviceProvider.UseSerializerMemoryPack();

            if ((commonStore.Modes & CommonModes.Server) == CommonModes.Server)
            {
                if ((modules & ExcludeModule.Action) != ExcludeModule.Action)
                    serviceProvider.UseActionServer();
                if ((modules & ExcludeModule.Forward) != ExcludeModule.Forward)
                    serviceProvider.UseForwardServer();
                if ((modules & ExcludeModule.SForward) != ExcludeModule.SForward)
                    serviceProvider.UseSForwardServer();
                if ((modules & ExcludeModule.Socks5) != ExcludeModule.Socks5)
                    serviceProvider.UseSocks5Server();
                if ((modules & ExcludeModule.Tuntap) != ExcludeModule.Tuntap)
                    serviceProvider.UseTuntapServer();
                if ((modules & ExcludeModule.Updater) != ExcludeModule.Updater)
                    serviceProvider.UseUpdaterServer();

                serviceProvider.UseAccessServer().UseDecenterServer().UsePcpServer().UseRelayServer()
                 .UseSignInServer().UseSyncServer().UseTunnelServer().UseFlowServer();

                serviceProvider.UseListen();
            }

            if ((commonStore.Modes & CommonModes.Client) == CommonModes.Client)
            {
                serviceProvider.UseLoggerClient();
                if ((modules & ExcludeModule.Api) != ExcludeModule.Api)
                    serviceProvider.UseApiClient();
                if ((modules & ExcludeModule.Action) != ExcludeModule.Action)
                    serviceProvider.UseActionClient();
                if ((modules & ExcludeModule.Forward) != ExcludeModule.Forward)
                    serviceProvider.UseForwardClient();
                if ((modules & ExcludeModule.SForward) != ExcludeModule.SForward)
                    serviceProvider.UseSForwardClient();
                if ((modules & ExcludeModule.Socks5) != ExcludeModule.Socks5)
                    serviceProvider.UseSocks5Client();
                if ((modules & ExcludeModule.Tuntap) != ExcludeModule.Tuntap)
                    serviceProvider.UseTuntapClient();
                if ((modules & ExcludeModule.Updater) != ExcludeModule.Updater)
                    serviceProvider.UseUpdaterClient();
                serviceProvider.UseExRoute().UseAccessClient().UseDecenterClient().UsePcpClient().UseRelayClient().UseSyncClient().UseTunnelClient().UseFlowClient();

                serviceProvider.UseSignInClient();
            }
        }

    }

    /// <summary>
    /// 排除那些模块
    /// </summary>
    [Flags]
    public enum ExcludeModule : uint
    {
        /// <summary>
        /// 无
        /// </summary>
        None = 0,
        /// <summary>
        /// 端口转发
        /// </summary>
        Forward = 1,
        /// <summary>
        /// 内网穿透
        /// </summary>
        SForward = 2,
        /// <summary>
        /// socks5
        /// </summary>
        Socks5 = 4,
        /// <summary>
        /// 虚拟网卡
        /// </summary>
        Tuntap = 8,
        /// <summary>
        /// 更新检测
        /// </summary>
        Updater = 16,
        /// <summary>
        /// 文件存储库
        /// </summary>
        StoreFile = 32,
        /// <summary>
        /// MemoryPack序列化库
        /// </summary>
        SerializerMemoryPack = 64,
        /// <summary>
        /// 管理接口和网页
        /// </summary>
        Api = 128,
        /// <summary>
        /// 自定义认证
        /// </summary>
        Action = 256,
    }
}
