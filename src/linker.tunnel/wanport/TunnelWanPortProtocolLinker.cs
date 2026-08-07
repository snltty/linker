using linker.libs;
using linker.libs.extends;
using linker.stun;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace linker.tunnel.wanport
{
    public class TunnelWanPortProtocolLinkerBase
    {
        protected Memory<byte> BuildSendData(byte[] buffer, byte i)
        {
            byte[] temp = Encoding.UTF8.GetBytes(Environment.TickCount64.ToString().Sha256().SubStr(0, new Random().Next(16, 32)));
            temp.AsMemory().CopyTo(buffer);
            buffer[0] = 0;
            buffer[1] = i;

            return buffer.AsMemory(0, 2 + temp.Length);
        }
        protected IPEndPoint UnpackRecvData(byte[] buffer, int length)
        {
            for (int j = 0; j < length; j++)
            {
                buffer[j] = (byte)(buffer[j] ^ byte.MaxValue);
            }
            AddressFamily addressFamily = (AddressFamily)buffer[0];
            int iplength = addressFamily == AddressFamily.InterNetwork ? 4 : 16;
            IPAddress ip = new IPAddress(buffer.AsSpan(1, iplength));
            ushort port = buffer.AsMemory(1 + iplength).ToUInt16();

            return new IPEndPoint(ip, port);
        }


        private readonly List<StunServer> stunServers = new List<StunServer>
        {
            new StunServer { Host = "linker.snltty.com", Port = 3478 },
            new StunServer { Host = "linker.snltty.com", Port = 3478 },
            new StunServer { Host = "stunserver2025.stunprotocol.org", Port = 3478 },
        };
        private readonly StunClient stun = new StunClient();
        protected async Task<TunnelWanPortEndPoint> TryStun(IPAddress ip)
        {
            try
            {
                stunServers[0] = new StunServer { Host = ip.ToString(), Port = 3478 };

                foreach (var server in stunServers)
                {
                    StunNatBehaviorResult result = await stun.DiscoverNatBehaviorAsync(server.Host, server.Port, new StunClientOptions
                    {
                        AddressFamilyMode = StunAddressFamilyMode.Ipv6Preferred,
                        MaxAttempts = 3
                    }).ConfigureAwait(false);
                    StunNatMappingBehavior mapping = result.MappingBehavior;
                    StunNatFilteringBehavior filtering = result.FilteringBehavior;

                    if (result.Binding.ReflexiveEndPoint is not null)
                    {
                        return new TunnelWanPortEndPoint
                        {
                            Local = result.Binding.LocalEndPoint,
                            Remote = result.Binding.ReflexiveEndPoint
                        };
                    }
                }
            }
            catch (Exception)
            {
            }
            return null;
        }

        record StunServer
        {
            public string Host { get; init; }
            public int Port { get; init; }
        }
    }

    /// <summary>
    /// 获取外网端口UDP
    /// </summary>
    public sealed class TunnelWanPortProtocolLinkerUdp : TunnelWanPortProtocolLinkerBase, ITunnelWanPortProtocol
    {
        public string Name => "Linker Udp";

        public TunnelWanPortProtocolType ProtocolType => TunnelWanPortProtocolType.Udp;

        public TunnelWanPortProtocolLinkerUdp()
        {
        }

        public async Task<TunnelWanPortEndPoint> GetAsync(IPEndPoint server)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(1024);
            try
            {
                for (byte i = 0; i < 5; i++)
                {
                    UdpClient udpClient = new UdpClient(server.AddressFamily);
                    udpClient.Client.ReuseBind(new IPEndPoint(server.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, 0));
                    udpClient.Client.WindowsUdpBug();
                    using CancellationTokenSource cts = new CancellationTokenSource(1000);
                    try
                    {
                        await udpClient.SendAsync(BuildSendData(buffer, i), server).ConfigureAwait(false);
                        UdpReceiveResult result = await udpClient.ReceiveAsync(cts.Token).ConfigureAwait(false);
                        if (result.Buffer.Length > 0)
                        {
                            return new TunnelWanPortEndPoint
                            {
                                Local = udpClient.Client.LocalEndPoint as IPEndPoint,
                                Remote = UnpackRecvData(result.Buffer, result.Buffer.Length)
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                            LoggerHelper.Instance.Error($"{Name}->{i}->{server}->{ex}");
                    }
                    finally
                    {
                        udpClient.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error($"{Name}->{server}->{ex}");
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }


            return await TryStun(server.Address).ConfigureAwait(false);
        }

    }

    /// <summary>
    /// 获取外网端口TCP
    /// </summary>
    public sealed class TunnelWanPortProtocolLinkerTcp : TunnelWanPortProtocolLinkerBase, ITunnelWanPortProtocol
    {
        public string Name => "Linker Tcp";

        public TunnelWanPortProtocolType ProtocolType => TunnelWanPortProtocolType.Tcp;

        public TunnelWanPortProtocolLinkerTcp()
        {
        }

        public async Task<TunnelWanPortEndPoint> GetAsync(IPEndPoint server)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(1024);
            using CancellationTokenSource cts = new CancellationTokenSource(5000);
            Socket socket = new Socket(server.AddressFamily, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
            socket.ReuseBind(new IPEndPoint(server.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, 0));
            try
            {
                await socket.ConnectAsync(server, cts.Token).ConfigureAwait(false);
                await socket.SendAsync(BuildSendData(buffer, (byte)new Random().Next(0, 255))).ConfigureAwait(false);

                int length = await socket.ReceiveAsync(buffer.AsMemory(), SocketFlags.None, cts.Token).ConfigureAwait(false);
                IPEndPoint localEP = socket.LocalEndPoint as IPEndPoint;

                return new TunnelWanPortEndPoint { Local = localEP, Remote = UnpackRecvData(buffer, length) };
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error($"{Name}->{ex}");
            }
            finally
            {
                socket.SafeClose();
                ArrayPool<byte>.Shared.Return(buffer);
            }

            return await TryStun(server.Address).ConfigureAwait(false);
        }
    }
}
