using Linker.Libs.Extends;
using System.Net;
using System.Net.Sockets;

namespace Linker.Tunnel.WanPort
{
    public sealed class TunnelWanPortLinker : ITunnelWanPort
    {
        public string Name => "默认";
        public TunnelWanPortType Type => TunnelWanPortType.Linker;

        public TunnelWanPortLinker()
        {
        }

        public async Task<TunnelWanPortEndPoint> GetAsync(IPEndPoint server)
        {
            UdpClient udpClient = new UdpClient(AddressFamily.InterNetwork);
            udpClient.Client.Reuse();
            udpClient.Client.WindowsUdpBug();

            for (int i = 0; i < 5; i++)
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

                    return new TunnelWanPortEndPoint { Local = udpClient.Client.LocalEndPoint as IPEndPoint, Remote = remoteEP };
                }
                catch (Exception)
                {
                }
            }

            return null;
        }
    }
}
