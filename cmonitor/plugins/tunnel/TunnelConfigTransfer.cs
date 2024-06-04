using cmonitor.client;
using cmonitor.config;
using cmonitor.plugins.tunnel.compact;
using cmonitor.plugins.tunnel.messenger;
using cmonitor.plugins.tunnel.transport;
using cmonitor.server;
using MemoryPack;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace cmonitor.plugins.tunnel
{
    public sealed class TunnelConfigTransfer
    {
        private readonly Config config;
        private readonly ClientSignInState clientSignInState;
        private readonly MessengerSender messengerSender;

        private uint version = 0;
        public uint ConfigVersion => version;
        private ConcurrentDictionary<string, TunnelTransportRouteLevelInfo> configs = new ConcurrentDictionary<string, TunnelTransportRouteLevelInfo>();
        public ConcurrentDictionary<string, TunnelTransportRouteLevelInfo> Config => configs;

        public TunnelConfigTransfer(Config config, ServiceProvider serviceProvider, ClientSignInState clientSignInState, MessengerSender messengerSender, TunnelCompactTransfer compactTransfer)
        {
            this.config = config;
            this.clientSignInState = clientSignInState;
            this.messengerSender = messengerSender;

            clientSignInState.NetworkEnabledHandle += (times) =>
            {
                GetRemoveRouteLevel();
            };
        }

        /// <summary>
        /// 刷新关于隧道的配置信息，也就是获取自己的和别的客户端的，方便查看
        /// </summary>
        public void RefreshConfig()
        {
            GetRemoveRouteLevel();
        }
        /// <summary>
        /// 修改自己的网关层级信息
        /// </summary>
        /// <param name="tunnelTransportConfigWrapInfo"></param>
        public void OnLocalRouteLevel(TunnelTransportRouteLevelInfo tunnelTransportConfigWrapInfo)
        {
            config.Data.Client.Tunnel.RouteLevelPlus = tunnelTransportConfigWrapInfo.RouteLevelPlus;
            config.Save();
            GetRemoveRouteLevel();
        }
        /// <summary>
        /// 收到别人发给我的修改我的信息
        /// </summary>
        /// <param name="tunnelTransportConfigWrapInfo"></param>
        /// <returns></returns>
        public TunnelTransportRouteLevelInfo OnRemoteRouteLevel(TunnelTransportRouteLevelInfo tunnelTransportConfigWrapInfo)
        {
            configs.AddOrUpdate(tunnelTransportConfigWrapInfo.MachineName, tunnelTransportConfigWrapInfo, (a, b) => tunnelTransportConfigWrapInfo);
            Interlocked.Increment(ref version);
            return GetLocalRouteLevel();
        }
        private void GetRemoveRouteLevel()
        {
            TunnelTransportRouteLevelInfo config = GetLocalRouteLevel();
            messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)TunnelMessengerIds.ConfigForward,
                Timeout = 10000,
                Payload = MemoryPackSerializer.Serialize(config)
            }).ContinueWith((result) =>
            {
                if (result.Result.Code == MessageResponeCodes.OK)
                {
                    List<TunnelTransportRouteLevelInfo> list = MemoryPackSerializer.Deserialize<List<TunnelTransportRouteLevelInfo>>(result.Result.Data.Span);
                    foreach (var item in list)
                    {
                        configs.AddOrUpdate(item.MachineName, item, (a, b) => item);
                    }
                    TunnelTransportRouteLevelInfo config = GetLocalRouteLevel();
                    configs.AddOrUpdate(config.MachineName, config, (a, b) => config);
                    Interlocked.Increment(ref version);
                }
            });
        }

        private TunnelTransportRouteLevelInfo GetLocalRouteLevel()
        {
            return new TunnelTransportRouteLevelInfo
            {
                MachineName = config.Data.Client.Name,
                RouteLevel = config.Data.Client.Tunnel.RouteLevel,
                RouteLevelPlus = config.Data.Client.Tunnel.RouteLevelPlus
            };
        }
        /// <summary>
        /// 收到别人发给我的修改我的打洞协议信息
        /// </summary>
        /// <param name="transports"></param>
        public void SetTransports(List<TunnelTransportItemInfo> transports)
        {
            config.Data.Client.Tunnel.TunnelTransports = transports;
            config.Save();
        }


        public List<TunnelTransportItemInfo> GetTransports()
        {
            return config.Data.Client.Tunnel.TunnelTransports;
        }
    }
}
