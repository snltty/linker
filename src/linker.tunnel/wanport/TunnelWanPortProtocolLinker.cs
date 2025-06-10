using linker.libs;
using linker.libs.extends;
using linker.libs.timer;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace linker.tunnel.wanport
{
    /// <summary>
    /// 获取外网端口UDP
    /// </summary>
    public sealed class TunnelWanPortProtocolLinkerUdp : ITunnelWanPortProtocol
    {
        public string Name => "Linker Udp";

        public TunnelWanPortProtocolType ProtocolType => TunnelWanPortProtocolType.Udp;

        public TunnelWanPortProtocolLinkerUdp()
        {

        }

        public async Task<TunnelWanPortEndPoint> GetAsync( IPEndPoint server)
        {
            UdpClient udpClient = new UdpClient(AddressFamily.InterNetwork);
            udpClient.Client.ReuseBind(new IPEndPoint(IPAddress.Any, 0));
            udpClient.Client.WindowsUdpBug();

            byte[] buffer = ArrayPool<byte>.Shared.Rent(1024);
            try
            {
                await udpClient.SendAsync(BuildSendData(buffer, 0), server).ConfigureAwait(false);
                TimerHelper.Async(async () =>
                {
                    for (byte i = 1; i < 10; i++)
                    {
                        await Task.Delay(15).ConfigureAwait(false);
                        await udpClient.SendAsync(BuildSendData(buffer, i), server).ConfigureAwait(false);
                    }
                });

                UdpReceiveResult result = await udpClient.ReceiveAsync().WaitAsync(TimeSpan.FromMilliseconds(2000)).ConfigureAwait(false);
                if (result.Buffer.Length == 0)
                {
                    return null;
                }

                for (int j = 0; j < result.Buffer.Length; j++)
                {
                    result.Buffer[j] = (byte)(result.Buffer[j] ^ byte.MaxValue);
                }
                AddressFamily addressFamily = (AddressFamily)result.Buffer[0];
                int length = addressFamily == AddressFamily.InterNetwork ? 4 : 16;
                IPAddress ip = new IPAddress(result.Buffer.AsSpan(1, length));
                ushort port = result.Buffer.AsMemory(1 + length).ToUInt16();

                IPEndPoint remoteEP = new IPEndPoint(ip, port);

                return new TunnelWanPortEndPoint { Local = udpClient.Client.LocalEndPoint as IPEndPoint, Remote = remoteEP };
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error($"{Name}->{ex}");
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
                udpClient.Close();
            }

            return null;
        }
        private static Memory<byte> BuildSendData(byte[] buffer, byte i)
        {
            byte[] temp = Encoding.UTF8.GetBytes(Environment.TickCount64.ToString().Md5().SubStr(0, new Random().Next(16, 32)));
            temp.AsMemory().CopyTo(buffer);
            buffer[0] = 0;
            buffer[1] = i;

            return buffer.AsMemory(0, temp.Length);
        }
    }

    /// <summary>
    /// 获取外网端口TCP
    /// </summary>
    public sealed class TunnelWanPortProtocolLinkerTcp : ITunnelWanPortProtocol
    {
        public string Name => "Linker Tcp";

        public TunnelWanPortProtocolType ProtocolType => TunnelWanPortProtocolType.Tcp;

        public TunnelWanPortProtocolLinkerTcp()
        {

        }

        public async Task<TunnelWanPortEndPoint> GetAsync(IPEndPoint server)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(1024);
            try
            {
                Socket socket = new Socket(server.AddressFamily, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
                socket.ReuseBind(new IPEndPoint(IPAddress.Any, 0));
                await socket.ConnectAsync(server).ConfigureAwait(false);

                await socket.SendAsync(BuildSendData(buffer, (byte)new Random().Next(0, 255))).ConfigureAwait(false);

                int length = await socket.ReceiveAsync(buffer.AsMemory(), SocketFlags.None).ConfigureAwait(false);
                for (int j = 0; j < length; j++)
                {
                    buffer[j] = (byte)(buffer[j] ^ byte.MaxValue);
                }

                AddressFamily addressFamily = (AddressFamily)buffer[0];
                int iplength = addressFamily == AddressFamily.InterNetwork ? 4 : 16;
                IPAddress ip = new IPAddress(buffer.AsSpan(1, iplength));
                ushort port = buffer.AsMemory(1 + iplength).ToUInt16();

                IPEndPoint remoteEP = new IPEndPoint(ip, port);
                IPEndPoint localEP = socket.LocalEndPoint as IPEndPoint;
                socket.Close();

                return new TunnelWanPortEndPoint { Local = localEP, Remote = remoteEP };
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error($"{Name}->{ex}");
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }

            return null;
        }

        private static Memory<byte> BuildSendData(byte[] buffer, byte i)
        {
            byte[] temp = Encoding.UTF8.GetBytes(Environment.TickCount64.ToString().Md5().SubStr(0, new Random().Next(16, 32)));
            temp.AsMemory().CopyTo(buffer);
            buffer[0] = 0;
            buffer[1] = i;

            return buffer.AsMemory(0, temp.Length);
        }
    }
}
