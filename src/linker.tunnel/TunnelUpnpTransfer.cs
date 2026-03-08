using linker.tunnel.transport;
using linker.upnp;
using System.Net.Sockets;

namespace linker.tunnel
{
    /// <summary>
    /// 给网关添加端口映射
    /// </summary>
    public sealed class TunnelUpnpTransfer
    {
        public MapInfo MapInfo { get; private set; }
        public MapInfo MapInfo1 { get; private set; }

        public MapInfo PortMap => MapInfo1 ?? MapInfo;

        private readonly TransportUdpPortMap transportUdpPortMap;
        private readonly TransportTcpPortMap transportTcpPortMap;
        public TunnelUpnpTransfer(TransportUdpPortMap transportUdpPortMap, TransportTcpPortMap transportTcpPortMap)
        {
            this.transportUdpPortMap = transportUdpPortMap;
            this.transportTcpPortMap = transportTcpPortMap;

            PortMappingUtility.StartDiscovery();
        }

        /// <summary>
        /// 设置端口映射，内网端口和外网端口一样
        /// </summary>
        /// <param name="privatePort"></param>
        public void SetMap(int privatePort)
        {
            DeleteMap();
            MapInfo = new MapInfo { PrivatePort = privatePort, PublicPort = privatePort };

            PortMappingInfo tcp = new PortMappingInfo { PrivatePort = privatePort, PublicPort = privatePort, ProtocolType = ProtocolType.Tcp, Description = $"linker tunnel tcp", DeviceType = DeviceType.All, LeaseDuration = 7 * 24 * 60 * 60, Deletable = false };
            _ = PortMappingUtility.Add(tcp).ConfigureAwait(false);
            PortMappingInfo udp = new PortMappingInfo { PrivatePort = privatePort, PublicPort = privatePort, ProtocolType = ProtocolType.Udp, Description = $"linker tunnel udp", DeviceType = DeviceType.All, LeaseDuration = 7 * 24 * 60 * 60, Deletable = false };
            _ = PortMappingUtility.Add(udp).ConfigureAwait(false);

            _ = transportTcpPortMap.Listen(privatePort).ConfigureAwait(false);
            _ = transportUdpPortMap.Listen(privatePort).ConfigureAwait(false);
        }
        /// <summary>
        /// 设置端口映射，内网端口和外网端口不一样
        /// </summary>
        /// <param name="privatePort"></param>
        /// <param name="publicPort"></param>
        public void SetMap(int privatePort, int publicPort)
        {

            DeleteMap();
            MapInfo1 = new MapInfo { PrivatePort = privatePort, PublicPort = publicPort };

            PortMappingInfo tcp = new PortMappingInfo { PrivatePort = privatePort, PublicPort = publicPort, ProtocolType = ProtocolType.Tcp, Description = $"linker tunnel tcp", DeviceType = DeviceType.All, LeaseDuration = 7 * 24 * 60 * 60, Deletable = false };
            _ = PortMappingUtility.Add(tcp).ConfigureAwait(false);
            PortMappingInfo udp = new PortMappingInfo { PrivatePort = privatePort, PublicPort = publicPort, ProtocolType = ProtocolType.Udp, Description = $"linker tunnel udp", DeviceType = DeviceType.All, LeaseDuration = 7 * 24 * 60 * 60, Deletable = false };
            _ = PortMappingUtility.Add(udp).ConfigureAwait(false);

            _ = transportTcpPortMap.Listen(privatePort).ConfigureAwait(false);
            _ = transportUdpPortMap.Listen(privatePort).ConfigureAwait(false);
        }

        private void DeleteMap()
        {
            if (MapInfo != null)
            {
                PortMappingUtility.Delete(MapInfo.PublicPort, ProtocolType.Tcp, true).ConfigureAwait(false);
                PortMappingUtility.Delete(MapInfo.PublicPort, ProtocolType.Udp, true).ConfigureAwait(false);
            }
            if (MapInfo1 != null)
            {
                PortMappingUtility.Delete(MapInfo1.PublicPort, ProtocolType.Tcp, true).ConfigureAwait(false);
                PortMappingUtility.Delete(MapInfo1.PublicPort, ProtocolType.Udp, true).ConfigureAwait(false);
            }
        }
    }

    public sealed class MapInfo
    {
        public int PrivatePort { get; set; }
        public int PublicPort { get; set; }
    }
}
