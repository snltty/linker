using cmonitor.plugins.tunnel.messenger;
using cmonitor.server;
using common.libs;
using common.libs.extends;
using MemoryPack;
using System.Net;
using System.Net.Sockets;

namespace cmonitor.plugins.tunnel.compact
{
    public sealed class CompactSelfHost : ICompact
    {
        public string Name => "self";

        private readonly TcpServer tcpServer;
        private readonly MessengerSender messengerSender;

        public CompactSelfHost(TcpServer tcpServer, MessengerSender messengerSender)
        {
            this.tcpServer = tcpServer;
            this.messengerSender = messengerSender;
        }

        public async Task<TunnelCompactIPEndPoint> GetTcpExternalIPAsync(IPEndPoint server)
        {
            Socket socket = new Socket(server.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Reuse(true);
            socket.IPv6Only(server.AddressFamily, false);
            await socket.ConnectAsync(server).WaitAsync(TimeSpan.FromSeconds(2));

            IConnection connection = tcpServer.BindReceive(socket);
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap { Connection = connection, MessengerId = (ushort)TunnelMessengerIds.ExternalIP });

            if (resp.Code != MessageResponeCodes.OK) return null;

            IPEndPoint local = socket.LocalEndPoint as IPEndPoint;
            connection.Disponse();
            TunnelExternalIPInfo tunnelExternalIPInfo = MemoryPackSerializer.Deserialize<TunnelExternalIPInfo>(resp.Data.Span);

            return new TunnelCompactIPEndPoint { Local = local, Remote = tunnelExternalIPInfo.ExternalIP };
        }

        public async Task<TunnelCompactIPEndPoint> GetUdpExternalIPAsync(IPEndPoint server)
        {
            using UdpClient udpClient = new UdpClient();
            udpClient.Client.Reuse(true);

            for (int i = 0; i < 10; i++)
            {
                try
                {
                    await udpClient.SendAsync(new byte[1] { 0 }, server);
                    UdpReceiveResult result = await udpClient.ReceiveAsync().WaitAsync(TimeSpan.FromSeconds(500));
                    if (result.Buffer.Length == 0)
                    {
                        return null;
                    }
                    IPEndPoint remoteEP = IPEndPoint.Parse(result.Buffer.AsSpan().GetString());

                    return new TunnelCompactIPEndPoint { Local = udpClient.Client.LocalEndPoint as IPEndPoint, Remote = remoteEP };
                }
                catch (Exception)
                {
                }
            }
            return null;
        }
    }
}
