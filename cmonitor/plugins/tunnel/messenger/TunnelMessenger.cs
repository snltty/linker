using cmonitor.config;
using cmonitor.plugins.signin.messenger;
using cmonitor.plugins.tunnel.transport;
using cmonitor.server;
using MemoryPack;
using System.Buffers;

namespace cmonitor.plugins.tunnel.messenger
{
    public sealed class TunnelClientMessenger : IMessenger
    {
        private readonly ITransport transport;
        private readonly Config config;
        public TunnelClientMessenger(ITransport transport, Config config)
        {
            this.transport = transport;
            this.config = config;
        }

        [MessengerId((ushort)TunnelMessengerIds.Begin)]
        public async Task Begin(IConnection connection)
        {
            TunnelTransportInfo tunnelTransportInfo = MemoryPackSerializer.Deserialize<TunnelTransportInfo>(connection.ReceiveRequestWrap.Payload.Span);
            tunnelTransportInfo.RouteLevel = config.Data.Client.Tunnel.RouteLevel;
            TunnelTransportInfo info = await transport.OnBegin(tunnelTransportInfo);
            connection.Write(MemoryPackSerializer.Serialize(info));
        }
    }


    public sealed class TunnelServerMessenger : IMessenger
    {
        private readonly MessengerSender messengerSender;
        private readonly SignCaching signCaching;
        public TunnelServerMessenger(MessengerSender messengerSender, SignCaching signCaching)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
        }

        [MessengerId((ushort)TunnelMessengerIds.BeginForward)]
        public async Task BeginForward(IConnection connection)
        {
            TunnelTransportInfo tunnelTransportInfo = MemoryPackSerializer.Deserialize<TunnelTransportInfo>(connection.ReceiveRequestWrap.Payload.Span);

            if (signCaching.Get(tunnelTransportInfo.ToMachineName, out SignCacheInfo cache))
            {
                MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    MessengerId = (ushort)TunnelMessengerIds.Begin,
                    Payload = connection.ReceiveRequestWrap.Payload
                });
                if (resp.Code == MessageResponeCodes.OK)
                {
                    byte[] bytes = ArrayPool<byte>.Shared.Rent(resp.Data.Length);
                    resp.Data.CopyTo(bytes);

                    connection.Write(bytes.AsMemory(0, resp.Data.Length));

                    ArrayPool<byte>.Shared.Return(bytes);
                }
            }
        }
    }
}
