using cmonitor.plugins.relay.messenger;
using cmonitor.server;
using common.libs;
using common.libs.extends;
using MemoryPack;
using System.Net.Sockets;
using System.Text;

namespace cmonitor.plugins.relay.transport
{
    public sealed class TransportSelfHost : ITransport
    {
        public string Name => "self";

        private readonly TcpServer tcpServer;
        private readonly MessengerSender messengerSender;
        private readonly Memory<byte> relayFlagData = Encoding.UTF8.GetBytes("snltty.relay").AsMemory();

        public TransportSelfHost(TcpServer tcpServer, MessengerSender messengerSender)
        {
            this.tcpServer = tcpServer;
            this.messengerSender = messengerSender;
        }

        public async Task<Socket> RelayAsync(RelayInfo relayInfo)
        {
            Socket socket = new Socket(relayInfo.Server.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Reuse(true);
            socket.IPv6Only(relayInfo.Server.AddressFamily, false);
            await socket.ConnectAsync(relayInfo.Server).WaitAsync(TimeSpan.FromSeconds(5));

            IConnection connection = tcpServer.BindReceive(socket);
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = connection,
                MessengerId = (ushort)RelayMessengerIds.RelayForward,
                Payload = MemoryPackSerializer.Serialize(relayInfo)
            });
            if (resp.Code != MessageResponeCodes.OK || resp.Data.Span.SequenceEqual(Helper.TrueArray) == false)
            {
                connection.Disponse();
                return null;
            }
            await socket.SendAsync(relayFlagData);
            await Task.Delay(10);
            return socket;
        }

        public async Task<Socket> OnBeginAsync(RelayInfo relayInfo)
        {
            Socket socket = new Socket(relayInfo.Server.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Reuse(true);
            socket.IPv6Only(relayInfo.Server.AddressFamily, false);
            await socket.ConnectAsync(relayInfo.Server).WaitAsync(TimeSpan.FromSeconds(5));

            IConnection connection = tcpServer.BindReceive(socket);
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = connection,
                MessengerId = (ushort)RelayMessengerIds.RelayForward,
                Payload = MemoryPackSerializer.Serialize(relayInfo)
            });
            if (resp.Code != MessageResponeCodes.OK || resp.Data.Span.SequenceEqual(Helper.TrueArray) == false)
            {
                connection.Disponse();
                return null;
            }
            await socket.SendAsync(relayFlagData);
            await Task.Delay(10);
            return socket;
        }
    }
}
