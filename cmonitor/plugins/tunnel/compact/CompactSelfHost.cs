using cmonitor.plugins.tunnel.server;
using common.libs.extends;
using MemoryPack;
using System.Net;
using System.Net.Sockets;

namespace cmonitor.plugins.tunnel.compact
{
    public sealed class CompactSelfHost : ICompact
    {
        public string Type => "self";

        public async Task<TunnelCompactIPEndPoint> GetTcpExternalIPAsync(IPEndPoint server)
        {
            Socket socket = new Socket(server.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Reuse(true);
            socket.IPv6Only(server.AddressFamily, false);
            await socket.ConnectAsync(server).WaitAsync(TimeSpan.FromSeconds(5));

            byte[] bytes = new byte[20];
            int length = await socket.ReceiveAsync(bytes.AsMemory(), SocketFlags.None);
            if (length == 0)
            {
                return null;
            }

            TunnelExternalIPInfo tunnelExternalIPInfo = MemoryPackSerializer.Deserialize<TunnelExternalIPInfo>(bytes.AsSpan(0,length));

            return new TunnelCompactIPEndPoint { Local = socket.LocalEndPoint as IPEndPoint, Remote = tunnelExternalIPInfo.ExternalIP };
        }

        public async Task<TunnelCompactIPEndPoint> GetUdpExternalIPAsync(IPEndPoint server)
        {

            using UdpClient udpClient = new UdpClient();
            udpClient.Client.Reuse(true);
            await udpClient.SendAsync(new byte[1] { 0 }, server);
            var result = await udpClient.ReceiveAsync().WaitAsync(TimeSpan.FromSeconds(5));
            if (result.Buffer.Length == 0)
            {
                return null;
            }

            TunnelExternalIPInfo tunnelExternalIPInfo = MemoryPackSerializer.Deserialize<TunnelExternalIPInfo>(result.Buffer);

            return new TunnelCompactIPEndPoint { Local = udpClient.Client.LocalEndPoint as IPEndPoint, Remote = tunnelExternalIPInfo.ExternalIP };
        }
    }
}
