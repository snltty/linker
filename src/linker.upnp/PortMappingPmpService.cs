using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace linker.upnp
{
    public sealed class PmpDevice : IPortMappingDevice
    {
        public DeviceType Type => DeviceType.Pmp;
       

        public IPAddress GatewayIp { get; set; }
        public IPAddress WanIp { get; set; }


        public async Task<List<PortMappingInfo>> Get()
        {
            return [];
        }
        public async Task<PortMappingInfo> Get(int port, ProtocolType protocolType)
        {
            return default;
        }

        public async Task<bool> Add(PortMappingInfo mapping)
        {
            if ((mapping.DeviceType & Type) != Type) return false;

            byte[] bytes = new byte[12];
            bytes[0] = 0;
            bytes[1] = (byte)(mapping.ProtocolType == ProtocolType.Tcp ? 2 : 1);
            bytes[2] = 0;
            bytes[3] = 0;

            BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(4), (ushort)mapping.PrivatePort);
            BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(6), (ushort)mapping.PublicPort);
            BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(8), mapping.LeaseDuration);


            using UdpClient client = CreateClient();
            try
            {

                for (int i = 0; i < 3; i++)
                {
                    await client.SendAsync(bytes, bytes.Length, new IPEndPoint(GatewayIp, 5351)).ConfigureAwait(false);
                }
                var _cts = new CancellationTokenSource(3000);
                while (_cts.IsCancellationRequested == false)
                {
                    UdpReceiveResult result = await client.ReceiveAsync(_cts.Token).ConfigureAwait(false);
                    byte version = result.Buffer[0];
                    byte opcode = result.Buffer[1];
                    ushort code = BinaryPrimitives.ReadUInt16BigEndian(result.Buffer.AsSpan(2));
                    uint time = BinaryPrimitives.ReadUInt32BigEndian(result.Buffer.AsSpan(4));
                    ushort privatePort = BinaryPrimitives.ReadUInt16BigEndian(result.Buffer.AsSpan(8));
                    ushort publicPort = BinaryPrimitives.ReadUInt16BigEndian(result.Buffer.AsSpan(10));
                    uint lease = BinaryPrimitives.ReadUInt32BigEndian(result.Buffer.AsSpan(12));

                    if (code == 0)
                    {
                        return true;
                    }
                }
            }
            catch (Exception)
            {
            }
            client?.Dispose();

            return false;
        }
        public async Task<bool> Delete(int publicPort, ProtocolType protocol)
        {
            return true;
        }

        private UdpClient CreateClient()
        {
            UdpClient client = new UdpClient();
            client.Client.SetSocketOption(SocketOptionLevel.Socket,
                SocketOptionName.ReuseAddress, true);
            client.Client.Bind(new IPEndPoint(IPAddress.Any, 5350));
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    const uint IOC_IN = 0x80000000;
                    int IOC_VENDOR = 0x18000000;
                    int SIO_UDP_CONNRESET = (int)(IOC_IN | IOC_VENDOR | 12);
                    client.Client.IOControl((int)SIO_UDP_CONNRESET, new byte[] { Convert.ToByte(false) }, null);
                }
                catch (Exception)
                {
                }
            }
            return client;
        }

        public override string ToString()
        {
            return $"PMP:{GatewayIp}->{WanIp}";
        }
    }
    public sealed class PortMappingPmpService : IPortMappingService
    {
        public DeviceType Type => DeviceType.Pmp;

        private readonly ConcurrentDictionary<string, IPortMappingDevice> pmpDevices = new();

        public async Task<List<IPortMappingDevice>> Discovery(CancellationToken token)
        {
            try
            {
                List<Task<PmpDevice>> tasks = GetGatewayIp().Select(async (ip) =>
                {
                    using UdpClient client = CreateClient();
                    try
                    {

                        for (int i = 0; i < 3; i++)
                        {
                            await client.SendAsync([0, 0], 2, new IPEndPoint(ip, 5351)).ConfigureAwait(false);
                        }
                        var _cts = new CancellationTokenSource(3000);
                        while (token.IsCancellationRequested == false && _cts.Token.IsCancellationRequested == false)
                        {
                            UdpReceiveResult result = await client.ReceiveAsync(_cts.Token).ConfigureAwait(false);
                            byte version = result.Buffer[0];
                            byte opcode = result.Buffer[1];
                            ushort code = BinaryPrimitives.ReadUInt16BigEndian(result.Buffer.AsSpan(2));
                            uint time = BinaryPrimitives.ReadUInt32BigEndian(result.Buffer.AsSpan(4));
                            IPAddress wanip = new IPAddress(result.Buffer.AsSpan(8));

                            if (code == 0)
                            {
                                return new PmpDevice
                                {
                                    GatewayIp = ip,
                                    WanIp = wanip
                                };
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }
                    client?.Dispose();

                    return null;

                }).ToList();
                var responses = await Task.WhenAll(tasks).ConfigureAwait(false);
                foreach (var device in responses.Where(c => c != null))
                {
                    string key = device.GatewayIp.ToString();
                    if(pmpDevices.TryGetValue(key,out IPortMappingDevice _device) == false)
                    {
                        _device = device;
                        pmpDevices.TryAdd(key, device);
                    }
                }
                return pmpDevices.Values.ToList();
            }
            catch (Exception)
            {
            }
            return [];
        }

        private List<IPAddress> GetGatewayIp()
        {
            return NetworkInterface.GetAllNetworkInterfaces().Where(c => c.OperationalStatus == OperationalStatus.Up)
                .SelectMany(c => c.GetIPProperties().GatewayAddresses)
                .Select(c => c.Address).Where(c => c.AddressFamily == AddressFamily.InterNetwork)
                .Where(c => c.Equals(IPAddress.Loopback) == false).ToList();
        }
        private UdpClient CreateClient()
        {
            UdpClient client = new UdpClient();
            client.Client.SetSocketOption(SocketOptionLevel.Socket,
                SocketOptionName.ReuseAddress, true);
            client.Client.Bind(new IPEndPoint(IPAddress.Any, 5350));
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    const uint IOC_IN = 0x80000000;
                    int IOC_VENDOR = 0x18000000;
                    int SIO_UDP_CONNRESET = (int)(IOC_IN | IOC_VENDOR | 12);
                    client.Client.IOControl((int)SIO_UDP_CONNRESET, new byte[] { Convert.ToByte(false) }, null);
                }
                catch (Exception)
                {
                }
            }
            return client;
        }

        public List<IPortMappingDevice> GetDevices()
        {
            return pmpDevices.Values.ToList<IPortMappingDevice>();
        }

        public async Task<List<PortMappingInfo>> Get()
        {
            return [];
        }
        public async Task<PortMappingInfo> Get(int port, ProtocolType protocolType)
        {
            return default;
        }

        public async Task Add(PortMappingInfo mapping)
        {
            foreach (var device in pmpDevices.Values)
            {
                await device.Add(mapping).ConfigureAwait(false);
            }
        }
        public async Task Delete(int publicPort, ProtocolType protocol)
        {
            foreach (var device in pmpDevices.Values)
            {
                await device.Delete(publicPort, protocol).ConfigureAwait(false);
            }
        }

    }
}
