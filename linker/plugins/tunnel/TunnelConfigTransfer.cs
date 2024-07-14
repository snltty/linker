using linker.client;
using linker.client.config;
using linker.config;
using linker.plugins.tunnel.messenger;
using linker.server;
using linker.tunnel.adapter;
using linker.tunnel.wanport;
using MemoryPack;
using System.Collections.Concurrent;

namespace linker.plugins.tunnel
{
    public sealed class TunnelConfigTransfer
    {
        private readonly FileConfig config;
        private readonly RunningConfig running;
        private readonly ClientSignInState clientSignInState;
        private readonly MessengerSender messengerSender;
        private readonly RunningConfigTransfer runningConfigTransfer;
        private readonly ITunnelAdapter tunnelAdapter;

        private string exipConfigKey = "excludeIPConfig";

        private uint version = 0;
        public uint ConfigVersion => version;
        private ConcurrentDictionary<string, TunnelTransportRouteLevelInfo> configs = new ConcurrentDictionary<string, TunnelTransportRouteLevelInfo>();
        public ConcurrentDictionary<string, TunnelTransportRouteLevelInfo> Config => configs;

        public TunnelConfigTransfer(FileConfig config, RunningConfig running, ClientSignInState clientSignInState, MessengerSender messengerSender, RunningConfigTransfer runningConfigTransfer, ITunnelAdapter tunnelAdapter)
        {
            this.config = config;
            this.running = running;
            this.clientSignInState = clientSignInState;
            this.messengerSender = messengerSender;
            this.runningConfigTransfer = runningConfigTransfer;
            this.tunnelAdapter = tunnelAdapter;

            clientSignInState.NetworkEnabledHandle += (times) =>
            {
                GetRemoveRouteLevel();
            };
            clientSignInState.NetworkFirstEnabledHandle += () =>
            {
                SyncExcludeIP();
            };
            InitConfig();

            runningConfigTransfer.Setter(exipConfigKey, SettExcludeIPs);
            runningConfigTransfer.Getter(exipConfigKey, () => MemoryPackSerializer.Serialize(GetExcludeIPs()));

        }
        private void InitConfig()
        {
            bool updateVersion = false;
            List<TunnelWanPortInfo> server = running.Data.Tunnel.Servers;
            if (server.FirstOrDefault(c => c.Type == TunnelWanPortType.Linker && c.ProtocolType == TunnelWanPortProtocolType.Udp) == null)
            {
                server.Add(new TunnelWanPortInfo
                {
                    Name = "Linker Udp",
                    Type = TunnelWanPortType.Linker,
                    ProtocolType = TunnelWanPortProtocolType.Udp,
                    Disabled = false,
                    Host = running.Data.Client.Servers.FirstOrDefault().Host,
                });
                updateVersion = true;
            }
            if (server.FirstOrDefault(c => c.Type == TunnelWanPortType.Linker && c.ProtocolType == TunnelWanPortProtocolType.Tcp) == null)
            {
                server.Add(new TunnelWanPortInfo
                {
                    Name = "Linker Tcp",
                    Type = TunnelWanPortType.Linker,
                    ProtocolType = TunnelWanPortProtocolType.Tcp,
                    Disabled = false,
                    Host = running.Data.Client.Servers.FirstOrDefault().Host,
                });
                updateVersion = true;
            }
            tunnelAdapter.SetTunnelWanPortProtocols(server, updateVersion);
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
        /// <param name="tunnelTransportFileConfigInfo"></param>
        public void OnLocalRouteLevel(TunnelTransportRouteLevelInfo tunnelTransportFileConfigInfo)
        {
            running.Data.Tunnel.RouteLevelPlus = tunnelTransportFileConfigInfo.RouteLevelPlus;
            running.Data.Update();
            GetRemoveRouteLevel();
        }
        /// <summary>
        /// 收到别人发给我的修改我的信息
        /// </summary>
        /// <param name="tunnelTransportFileConfigInfo"></param>
        /// <returns></returns>
        public TunnelTransportRouteLevelInfo OnRemoteRouteLevel(TunnelTransportRouteLevelInfo tunnelTransportFileConfigInfo)
        {
            configs.AddOrUpdate(tunnelTransportFileConfigInfo.MachineId, tunnelTransportFileConfigInfo, (a, b) => tunnelTransportFileConfigInfo);
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


        private void SyncExcludeIP()
        {
            runningConfigTransfer.Sync(exipConfigKey, MemoryPackSerializer.Serialize(running.Data.Tunnel.ExcludeIPs));
        }
        public ExcludeIPItem[] GetExcludeIPs()
        {
            return running.Data.Tunnel.ExcludeIPs;
        }
        public void SettExcludeIPs(ExcludeIPItem[] ips)
        {
            running.Data.Tunnel.ExcludeIPs = ips;
            running.Data.Update();
            runningConfigTransfer.IncrementVersion(exipConfigKey);
            SyncExcludeIP();
        }
        private void SettExcludeIPs(Memory<byte> data)
        {
            running.Data.Tunnel.ExcludeIPs = MemoryPackSerializer.Deserialize<ExcludeIPItem[]>(data.Span);
            running.Data.Update();
        }
    }
}
