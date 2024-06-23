using link.client;
using link.client.config;
using link.config;
using link.plugins.tunnel.messenger;
using link.server;
using link.tunnel.compact;
using MemoryPack;
using System.Collections.Concurrent;

namespace link.plugins.tunnel
{
    public sealed class TunnelConfigTransfer
    {
        private readonly Config config;
        private readonly RunningConfig running;
        private readonly ClientSignInState clientSignInState;
        private readonly MessengerSender messengerSender;

        private uint version = 0;
        public uint ConfigVersion => version;
        private ConcurrentDictionary<string, TunnelTransportRouteLevelInfo> configs = new ConcurrentDictionary<string, TunnelTransportRouteLevelInfo>();
        public ConcurrentDictionary<string, TunnelTransportRouteLevelInfo> Config => configs;

        public TunnelConfigTransfer(Config config, RunningConfig running, ClientSignInState clientSignInState, MessengerSender messengerSender, TunnelCompactTransfer compactTransfer)
        {
            this.config = config;
            this.running = running;
            this.clientSignInState = clientSignInState;
            this.messengerSender = messengerSender;

            clientSignInState.NetworkEnabledHandle += (times) =>
            {
                GetRemoveRouteLevel();
            };

            if (running.Data.Tunnel.Servers.Length == 0)
            {
                running.Data.Tunnel.Servers = new TunnelCompactInfo[]
                {
                     new TunnelCompactInfo{
                         Name="默认",
                         Type= TunnelCompactType.Link,
                         Disabled = false,
                         Host = running.Data.Client.Servers.FirstOrDefault().Host
                     }
                };
            }
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
            running.Data.Tunnel.RouteLevelPlus = tunnelTransportConfigWrapInfo.RouteLevelPlus;
            running.Data.Update();
            GetRemoveRouteLevel();
        }
        /// <summary>
        /// 收到别人发给我的修改我的信息
        /// </summary>
        /// <param name="tunnelTransportConfigWrapInfo"></param>
        /// <returns></returns>
        public TunnelTransportRouteLevelInfo OnRemoteRouteLevel(TunnelTransportRouteLevelInfo tunnelTransportConfigWrapInfo)
        {
            configs.AddOrUpdate(tunnelTransportConfigWrapInfo.MachineId, tunnelTransportConfigWrapInfo, (a, b) => tunnelTransportConfigWrapInfo);
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
                        configs.AddOrUpdate(item.MachineId, item, (a, b) => item);
                    }
                    TunnelTransportRouteLevelInfo config = GetLocalRouteLevel();
                    configs.AddOrUpdate(config.MachineId, config, (a, b) => config);
                    Interlocked.Increment(ref version);
                }
            });
        }

        private TunnelTransportRouteLevelInfo GetLocalRouteLevel()
        {
            return new TunnelTransportRouteLevelInfo
            {
                MachineId = config.Data.Client.Id,
                RouteLevel = config.Data.Client.Tunnel.RouteLevel,
                RouteLevelPlus = running.Data.Tunnel.RouteLevelPlus
            };
        }

    }
}
