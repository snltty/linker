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
                //流量统计
                .AddFlowClient().AddFlowServer()

                //信标
                .AddMessenger()
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
        /// 运行起来
        /// </summary>
        /// <returns></returns>
        public static void Build()
        {
            if (builded.StartOperation() == false) return;

            serviceProvider = serviceCollection.BuildServiceProvider();
            serviceProvider.UseMessenger().UseStoreFile().UseSerializerMemoryPack();

            ICommonStore commonStore = serviceProvider.GetService<ICommonStore>();
            if ((commonStore.Modes & CommonModes.Server) == CommonModes.Server)
            {
                serviceProvider.UseAccessServer().UseActionServer().UseDecenterServer().UseForwardServer().UsePcpServer().UseRelayServer().UseSForwardServer().UseSignInServer().UseSocks5Server().UseSyncServer().UseTunnelServer().UseTuntapServer().UseUpdaterServer().UseFlowServer();

                serviceProvider.UseListen();
            }

            if ((commonStore.Modes & CommonModes.Client) == CommonModes.Client)
            {
                serviceProvider.UseLoggerClient().UseApiClient().UseExRoute().UseAccessClient().UseActionClient().UseDecenterClient().UseForwardClient().UsePcpClient().UseRelayClient().UseSForwardClient().UseSocks5Client().UseSyncClient().UseTunnelClient().UseTuntapClient().UseUpdaterClient().UseFlowClient();

                serviceProvider.UseSignInClient();
            }
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

    }
}
