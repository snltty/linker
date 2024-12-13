using linker.libs;
using Mono.Nat;
using System.Collections.Concurrent;

namespace linker.tunnel
{
    public sealed class TunnelUpnpTransfer
    {

        private readonly SemaphoreSlim locker = new SemaphoreSlim(1, 1);
        private readonly ConcurrentDictionary<NatProtocol, INatDevice> natDevices = new ConcurrentDictionary<NatProtocol, INatDevice>();

        public MapInfo MapInfo { get; private set; }
        public MapInfo MapInfo1 { get; private set; }

        public MapInfo PortMap => MapInfo1 ?? MapInfo;

        public TunnelUpnpTransfer()
        {
            NatUtility.DeviceFound += DeviceFound;
            NatUtility.StartDiscovery();
            LoopDiscovery();
        }

        private void LoopDiscovery()
        {
            TimerHelper.SetInterval(() =>
            {
                NatUtility.StopDiscovery();
                NatUtility.StartDiscovery();

                return true;
            }, 60 * 1000);
        }
        private void DeviceFound(object sender, DeviceEventArgs args)
        {
            INatDevice device = args.Device;

            if (natDevices.Count == 0 || natDevices.TryGetValue(device.NatProtocol, out INatDevice _device))
            {
                natDevices.AddOrUpdate(device.NatProtocol, device, (a, b) => device);
            }
            AddMap();
        }
        private void AddMap()
        {
            if (natDevices.Count == 0 || MapInfo == null) return;

            TimerHelper.Async(async () =>
            {
                await locker.WaitAsync();

                INatDevice device = natDevices.FirstOrDefault().Value;

                try
                {
                    if (await HasMap(device, Protocol.Tcp, MapInfo.PublicPort) == false)
                    {
                        Mapping mapping = new Mapping(Protocol.Tcp, MapInfo.PrivatePort, MapInfo.PublicPort, 720, $"linker-tcp-{MapInfo.PublicPort}-{MapInfo.PrivatePort}");
                        await device.CreatePortMapAsync(mapping);
                        Mapping m = await device.GetSpecificMappingAsync(Protocol.Tcp, mapping.PublicPort);
                    }
                }
                catch
                {
                }

                try
                {
                    if (await HasMap(device, Protocol.Udp, MapInfo.PublicPort) == false)
                    {
                        Mapping mapping = new Mapping(Protocol.Udp, MapInfo.PrivatePort, MapInfo.PublicPort, 720, $"linker-udp-{MapInfo.PublicPort}-{MapInfo.PrivatePort}");
                        await device.CreatePortMapAsync(mapping);
                        Mapping m = await device.GetSpecificMappingAsync(Protocol.Udp, mapping.PublicPort);
                    }
                }
                catch
                {
                }

                locker.Release();
            });
        }
        private async Task<bool> HasMap(INatDevice device, Protocol protocol, int publicPort)
        {
            try
            {
                Mapping m = await device.GetSpecificMappingAsync(protocol, publicPort);
                return true;
            }
            catch (Exception)
            {
            }
            return false;
        }

        public void SetMap(int privatePort)
        {
            MapInfo = new MapInfo { PrivatePort = privatePort, PublicPort = privatePort };
            AddMap();
        }
        public void SetMap(int privatePort, int publicPort)
        {
            MapInfo1 = new MapInfo { PrivatePort = privatePort, PublicPort = publicPort };
        }
    }

    public sealed class MapInfo
    {
        public int PrivatePort { get; set; }
        public int PublicPort { get; set; }
    }
}
