using linker.libs.timer;
using linker.tunnel.transport;
using Mono.Nat;
using System.Collections.Concurrent;

namespace linker.tunnel
{
    /// <summary>
    /// 给网关添加端口映射
    /// </summary>
    public sealed class TunnelUpnpTransfer
    {

        private readonly SemaphoreSlim locker = new SemaphoreSlim(1, 1);
        private readonly ConcurrentDictionary<NatProtocol, INatDevice> natDevices = new ConcurrentDictionary<NatProtocol, INatDevice>();

        public MapInfo MapInfo { get; private set; }
        public MapInfo MapInfo1 { get; private set; }

        public MapInfo PortMap => MapInfo1 ?? MapInfo;

        private readonly TransportUdpPortMap transportUdpPortMap;
        private readonly TransportTcpPortMap transportTcpPortMap;
        public TunnelUpnpTransfer(TransportUdpPortMap transportUdpPortMap, TransportTcpPortMap transportTcpPortMap)
        {
            this.transportUdpPortMap = transportUdpPortMap;
            this.transportTcpPortMap = transportTcpPortMap;

            NatUtility.DeviceFound += DeviceFound;
            NatUtility.StartDiscovery();
            LoopDiscovery();

        }

        private static void LoopDiscovery()
        {
            TimerHelper.SetIntervalLong(() =>
            {
                NatUtility.StopDiscovery();
                NatUtility.StartDiscovery();
            }, 60 * 1000);
        }
        private void DeviceFound(object sender, DeviceEventArgs args)
        {
            INatDevice device = args.Device;

            natDevices.AddOrUpdate(device.NatProtocol, device, (a, b) => device);

            AddMap();
        }
        private void AddMap()
        {
            if (natDevices.IsEmpty || MapInfo == null) return;

            TimerHelper.Async(async () =>
            {
                await locker.WaitAsync().ConfigureAwait(false);

                foreach (var device in natDevices.Values)
                {
                    try
                    {
                        if (await HasMap(device, Protocol.Tcp, MapInfo.PublicPort).ConfigureAwait(false) == false)
                        {
                            Mapping mapping = new Mapping(Protocol.Tcp, MapInfo.PrivatePort, MapInfo.PublicPort, 7 * 24 * 60 * 60, $"linker-tcp-{MapInfo.PublicPort}-{MapInfo.PrivatePort}");
                            await device.CreatePortMapAsync(mapping).ConfigureAwait(false);
                            Mapping m = await device.GetSpecificMappingAsync(Protocol.Tcp, mapping.PublicPort).ConfigureAwait(false);
                        }
                    }
                    catch
                    {
                    }

                    try
                    {
                        if (await HasMap(device, Protocol.Udp, MapInfo.PublicPort).ConfigureAwait(false) == false)
                        {
                            Mapping mapping = new Mapping(Protocol.Udp, MapInfo.PrivatePort, MapInfo.PublicPort, 7 * 24 * 60 * 60, $"linker-udp-{MapInfo.PublicPort}-{MapInfo.PrivatePort}");
                            await device.CreatePortMapAsync(mapping).ConfigureAwait(false);
                            Mapping m = await device.GetSpecificMappingAsync(Protocol.Udp, mapping.PublicPort).ConfigureAwait(false);
                        }
                    }
                    catch
                    {
                    }
                }
                locker.Release();
            });
        }
        private async Task<bool> HasMap(INatDevice device, Protocol protocol, int publicPort)
        {
            try
            {
                Mapping m = await device.GetSpecificMappingAsync(protocol, publicPort).ConfigureAwait(false);
                return (DateTime.Now - m.Expiration).TotalSeconds > 2 * 60;
            }
            catch (Exception)
            {
            }
            return false;
        }

        /// <summary>
        /// 设置端口映射，内网端口和外网端口一样
        /// </summary>
        /// <param name="privatePort"></param>
        public void SetMap(int privatePort)
        {
            MapInfo = new MapInfo { PrivatePort = privatePort, PublicPort = privatePort };
            AddMap();

            _ = transportTcpPortMap.Listen(privatePort);
            _ = transportUdpPortMap.Listen(privatePort);
        }
        /// <summary>
        /// 设置端口映射，内网端口和外网端口不一样
        /// </summary>
        /// <param name="privatePort"></param>
        /// <param name="publicPort"></param>
        public void SetMap(int privatePort, int publicPort)
        {
            MapInfo1 = new MapInfo { PrivatePort = privatePort, PublicPort = publicPort };

            _ = transportTcpPortMap.Listen(privatePort);
            _ = transportUdpPortMap.Listen(privatePort);
        }
    }

    public sealed class MapInfo
    {
        public int PrivatePort { get; set; }
        public int PublicPort { get; set; }
    }
}
