using common.libs;
using common.libs.extends;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace cmonitor.plugins.tunnel.compact
{
    public sealed class TunnelCompactSelfHost : ITunnelCompact
    {
        public string Name => "默认";
        public TunnelCompactType Type => TunnelCompactType.Self;

        public static UdpClient udpClient;

        public TunnelCompactSelfHost()
        {
        }

        public async Task<TunnelCompactIPEndPoint> GetExternalIPAsync(IPEndPoint server)
        {
            udpClient = new UdpClient(AddressFamily.InterNetwork);
            //udpClient.Client.IPv6Only(AddressFamily.InterNetworkV6, false);
            udpClient.Client.Reuse(true);
            udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, new Random().Next(10000, 50000)));
            

            for (int i = 0; i < 10; i++)
            {
                try
                {
                    await udpClient.SendAsync(new byte[1] { 0 }, server);
                    UdpReceiveResult result = await udpClient.ReceiveAsync().WaitAsync(TimeSpan.FromMilliseconds(500));
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

                    return new TunnelCompactIPEndPoint { Local = udpClient.Client.LocalEndPoint as IPEndPoint, Remote = remoteEP };
                }
                catch (Exception)
                {
                }
            }

            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        UdpReceiveResult result = await udpClient.ReceiveAsync();
                        Logger.Instance.Error(Encoding.UTF8.GetString(result.Buffer));
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Error(ex);
                    }
                }
            });

            return null;
        }
    }
}
