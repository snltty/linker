using common.libs.extends;
using System.Net;
using System.Net.Sockets;

namespace cmonitor.plugins.tunnel.compact
{
    public sealed class CompactSelfHost : ICompact
    {
        public string Type => "self";

        public async Task<CompactIPEndPoint> GetTcpExternalIPAsync(IPEndPoint server)
        {
            Socket socket = new Socket(server.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Reuse(true);
            socket.IPv6Only(server.AddressFamily, false);
            await socket.ConnectAsync(server).WaitAsync(TimeSpan.FromSeconds(5));

            byte[] bytes = new byte[20];
            int length = await socket.ReceiveAsync(bytes.AsMemory(), SocketFlags.None);
            if (length == 0) return null;

            return new CompactIPEndPoint { Local = socket.LocalEndPoint as IPEndPoint, Remote = ReadData(bytes) };
        }

        public async Task<CompactIPEndPoint> GetUdpExternalIPAsync(IPEndPoint server)
        {

            using UdpClient udpClient = new UdpClient();
            udpClient.Client.Reuse(true);
            await udpClient.SendAsync(new byte[1] { 0 }, server);
            var result = await udpClient.ReceiveAsync().WaitAsync(TimeSpan.FromSeconds(5));
            if (result.Buffer.Length == 0) return null;

            return new CompactIPEndPoint { Local = udpClient.Client.LocalEndPoint as IPEndPoint, Remote = ReadData(result.Buffer) };
        }

        private IPEndPoint ReadData(byte[] bytes)
        {
            int index = 0;
            AddressFamily family = (AddressFamily)bytes[index];
            index++;

            int ipLength = family switch
            {
                AddressFamily.InterNetwork => 4,
                AddressFamily.InterNetworkV6 => 16,
                _ => 0,
            };

            if (ipLength > 0)
            {
                IPAddress ip = new IPAddress(bytes.AsSpan(0, ipLength));
                index += ipLength;

                ushort port = bytes.AsSpan(index).ToUInt16();
                IPEndPoint iPEndPoint = new IPEndPoint(ip, port);
                return iPEndPoint;
            }

            return null;
        }
    }
}
