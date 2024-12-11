using linker.client.config;
using linker.config;
using linker.libs;
using linker.plugins.client;
using linker.plugins.decenter;
using linker.plugins.messenger;
using linker.tunnel;
using linker.tunnel.transport;
using MemoryPack;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Quic;
using System.Security.Cryptography.X509Certificates;

namespace linker.plugins.tunnel
{
    public sealed class TunnelConfigTransfer : IDecenter
    {
        public string Name => "tunnel";
        public VersionManager DataVersion { get; } = new VersionManager();

        public int RouteLevel => config.Data.Client.Tunnel.RouteLevel + running.Data.Tunnel.RouteLevelPlus;
        public IPAddress[] LocalIPs => config.Data.Client.Tunnel.LocalIPs;
        public IPAddress[] RouteIPs => config.Data.Client.Tunnel.RouteIPs;
        public List<TunnelTransportItemInfo> Transports => config.Data.Client.Tunnel.Transports;
        public X509Certificate2 Certificate { get; private set; }

        private readonly FileConfig config;
        private readonly RunningConfig running;
        private readonly ClientSignInState clientSignInState;
        private readonly IMessengerSender messengerSender;
        private readonly TunnelUpnpTransfer upnpTransfer;
        private readonly ClientConfigTransfer clientConfigTransfer;

        public VersionManager Version { get; } = new VersionManager();
        public ConcurrentDictionary<string, TunnelTransportRouteLevelInfo> Config { get; } = new ConcurrentDictionary<string, TunnelTransportRouteLevelInfo>();

        public TunnelConfigTransfer(FileConfig config, RunningConfig running, ClientSignInState clientSignInState, IMessengerSender messengerSender, TunnelUpnpTransfer upnpTransfer, ClientConfigTransfer clientConfigTransfer)
        {
            this.config = config;
            this.running = running;
            this.clientSignInState = clientSignInState;
            this.messengerSender = messengerSender;
            this.upnpTransfer = upnpTransfer;
            this.clientConfigTransfer = clientConfigTransfer;

            string path = Path.GetFullPath(clientConfigTransfer.SSL.File);
            if (File.Exists(path))
            {
                Certificate = new X509Certificate2(path, clientConfigTransfer.SSL.Password, X509KeyStorageFlags.Exportable);
            }
            clientSignInState.NetworkEnabledHandle += (times) =>
            {
                TimerHelper.Async(RefreshRouteLevel);
                RefreshPortMap();
            };
            TestQuic();
        }

        public Memory<byte> GetData()
        {
            TunnelTransportRouteLevelInfo tunnelTransportRouteLevelInfo = GetLocalRouteLevel();
            Config.AddOrUpdate(tunnelTransportRouteLevelInfo.MachineId, tunnelTransportRouteLevelInfo, (a, b) => tunnelTransportRouteLevelInfo);
            Version.Add();
            return MemoryPackSerializer.Serialize(tunnelTransportRouteLevelInfo);
        }
        public void SetData(Memory<byte> data)
        {
            TunnelTransportRouteLevelInfo tunnelTransportRouteLevelInfo = MemoryPackSerializer.Deserialize<TunnelTransportRouteLevelInfo>(data.Span);
            Config.AddOrUpdate(tunnelTransportRouteLevelInfo.MachineId, tunnelTransportRouteLevelInfo, (a, b) => tunnelTransportRouteLevelInfo);
            Version.Add();
        }
        public void SetData(List<ReadOnlyMemory<byte>> data)
        {
            List<TunnelTransportRouteLevelInfo> list = data.Select(c => MemoryPackSerializer.Deserialize<TunnelTransportRouteLevelInfo>(c.Span)).ToList();
            foreach (var item in list)
            {
                Config.AddOrUpdate(item.MachineId, item, (a, b) => item);
            }
            TunnelTransportRouteLevelInfo config = GetLocalRouteLevel();
            Config.AddOrUpdate(config.MachineId, config, (a, b) => config);
            Version.Add();
        }

        public void SetTransports(List<TunnelTransportItemInfo> transports)
        {
            config.Data.Client.Tunnel.Transports = transports;
            config.Data.Update();
        }
     
        /// <summary>
        /// 刷新网关等级数据
        /// </summary>
        public void RefreshRouteLevel()
        {
            config.Data.Client.Tunnel.RouteLevel = NetworkHelper.GetRouteLevel(clientConfigTransfer.Server.Host, out List<IPAddress> ips);
            config.Data.Client.Tunnel.RouteIPs = ips.ToArray();
            config.Data.Client.Tunnel.LocalIPs = NetworkHelper.GetIPV6().Concat(NetworkHelper.GetIPV4()).ToArray();
            DataVersion.Add();
        }

        /// <summary>
        /// 刷新关于隧道的配置信息，也就是获取自己的和别的客户端的，方便查看
        /// </summary>
        public void RefreshConfig()
        {
            DataVersion.Add();
        }
        /// <summary>
        /// 修改自己的网关层级信息
        /// </summary>
        /// <param name="tunnelTransportFileConfigInfo"></param>
        public void OnLocalRouteLevel(TunnelTransportRouteLevelInfo tunnelTransportFileConfigInfo)
        {
            running.Data.Tunnel.RouteLevelPlus = tunnelTransportFileConfigInfo.RouteLevelPlus;
            running.Data.Tunnel.PortMapWan = tunnelTransportFileConfigInfo.PortMapWan;
            running.Data.Tunnel.PortMapLan = tunnelTransportFileConfigInfo.PortMapLan;
            running.Data.Update();
            GetData();
            DataVersion.Add();
        }
        private TunnelTransportRouteLevelInfo GetLocalRouteLevel()
        {
            return new TunnelTransportRouteLevelInfo
            {
                MachineId = clientConfigTransfer.Id,
                RouteLevel = config.Data.Client.Tunnel.RouteLevel,
                RouteLevelPlus = running.Data.Tunnel.RouteLevelPlus,
                PortMapWan = running.Data.Tunnel.PortMapWan,
                PortMapLan = running.Data.Tunnel.PortMapLan,
                NeedReboot = false
            };
        }

        private void TestQuic()
        {
            if (OperatingSystem.IsWindows())
            {
                if (QuicListener.IsSupported == false)
                {
                    try
                    {
                        if (File.Exists("msquic-openssl.dll"))
                        {
                            LoggerHelper.Instance.Warning($"copy msquic-openssl.dll -> msquic.dll，please restart linker");
                            File.Move("msquic.dll", "msquic.dll.temp", true);
                            File.Move("msquic-openssl.dll", "msquic.dll", true);

                            if (Environment.UserInteractive == false && OperatingSystem.IsWindows())
                            {
                                Environment.Exit(1);
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
                try
                {
                    if (File.Exists("msquic.dll.temp"))
                    {
                        File.Delete("msquic.dll.temp");
                    }
                    if (File.Exists("msquic-openssl.dll"))
                    {
                        File.Delete("msquic-openssl.dll");
                    }
                }
                catch (Exception)
                {
                }
            }
        }
        private void RefreshPortMap()
        {
            if (running.Data.Tunnel.PortMapLan > 0)
            {
                upnpTransfer.SetMap(running.Data.Tunnel.PortMapLan, running.Data.Tunnel.PortMapWan);
            }
            else
            {
                upnpTransfer.SetMap(clientSignInState.Connection.LocalAddress.Address, 18180);
            }
        }
    }
}
