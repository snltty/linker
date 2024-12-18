using linker.config;
using linker.tunnel;
using linker.libs;
using linker.messenger;
using linker.messenger.tunnel;
using linker.messenger.signin;

namespace linker.plugins.tunnel
{
    public sealed class PlusTunnelClientMessenger : TunnelClientMessenger, IMessenger
    {
        private readonly TunnelTransfer tunnel;
        private readonly TunnelConfigTransfer tunnelConfigTransfer;
        private readonly IMessengerSender messengerSender;
        private readonly ISerializer serializer;

        public PlusTunnelClientMessenger(TunnelTransfer tunnel, IMessengerSender messengerSender, ISerializer serializer, TunnelConfigTransfer tunnelConfigTransfer)
            : base(tunnel, messengerSender, serializer)
        {
            this.tunnel = tunnel;
            this.messengerSender = messengerSender;
            this.serializer = serializer;
            this.tunnelConfigTransfer = tunnelConfigTransfer;
        }

        [MessengerId((ushort)TunnelMessengerIds.RouteLevel)]
        public void RouteLevel(IConnection connection)
        {
            TunnelTransportRouteLevelInfo tunnelTransportFileConfigInfo = serializer.Deserialize<TunnelTransportRouteLevelInfo>(connection.ReceiveRequestWrap.Payload.Span);
            tunnelConfigTransfer.OnLocalRouteLevel(tunnelTransportFileConfigInfo);
        }

    }

    public sealed class PlusTunnelServerMessenger : TunnelServerMessenger, IMessenger
    {
        private readonly SignCaching signCaching;
        private readonly IMessengerSender messengerSender;
        private readonly ISerializer serializer;

        public PlusTunnelServerMessenger(SignCaching signCaching, IMessengerSender messengerSender, ISerializer serializer)
            : base(messengerSender, signCaching, serializer)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
            this.serializer = serializer;
        }


        [MessengerId((ushort)TunnelMessengerIds.RouteLevelForward)]
        public async Task RouteLevelForward(IConnection connection)
        {
            TunnelTransportRouteLevelInfo tunnelTransportInfo = serializer.Deserialize<TunnelTransportRouteLevelInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(tunnelTransportInfo.MachineId, out SignCacheInfo cache) && signCaching.TryGet(connection.Id, out SignCacheInfo cache1) && cache.GroupId == cache1.GroupId)
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    MessengerId = (ushort)TunnelMessengerIds.RouteLevel,
                    Payload = connection.ReceiveRequestWrap.Payload
                }).ConfigureAwait(false);
            }

        }

    }
}
