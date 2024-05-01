using cmonitor.plugins.signin.messenger;
using cmonitor.plugins.tunnel.transport;
using cmonitor.server;
using common.libs;
using MemoryPack;

namespace cmonitor.plugins.tunnel.messenger
{
    public sealed class TunnelClientMessenger : IMessenger
    {
        private readonly TunnelTransfer tunnel;

        public TunnelClientMessenger(TunnelTransfer tunnel)
        {
            this.tunnel = tunnel;
        }

        [MessengerId((ushort)TunnelMessengerIds.Begin)]
        public void Begin(IConnection connection)
        {
            TunnelTransportInfo tunnelTransportInfo = MemoryPackSerializer.Deserialize<TunnelTransportInfo>(connection.ReceiveRequestWrap.Payload.Span);
            TunnelTransportExternalIPInfo local = tunnelTransportInfo.Local;
            tunnelTransportInfo.Local = tunnelTransportInfo.Remote;
            tunnelTransportInfo.Remote = local;

            tunnel.OnBegin(tunnelTransportInfo);
            connection.Write(Helper.TrueArray);
        }

        [MessengerId((ushort)TunnelMessengerIds.Info)]
        public async Task Info(IConnection connection)
        {
            TunnelTransportExternalIPRequestInfo request = MemoryPackSerializer.Deserialize<TunnelTransportExternalIPRequestInfo>(connection.ReceiveRequestWrap.Payload.Span);
            TunnelTransportExternalIPInfo tunnelTransportPortInfo = await tunnel.Info(request);
            if (tunnelTransportPortInfo != null)
            {
                connection.Write(MemoryPackSerializer.Serialize(tunnelTransportPortInfo));
            }
        }

        [MessengerId((ushort)TunnelMessengerIds.Fail)]
        public void Fail(IConnection connection)
        {
            TunnelTransportInfo tunnelTransportInfo = MemoryPackSerializer.Deserialize<TunnelTransportInfo>(connection.ReceiveRequestWrap.Payload.Span);
            TunnelTransportExternalIPInfo local = tunnelTransportInfo.Local;
            tunnelTransportInfo.Local = tunnelTransportInfo.Remote;
            tunnelTransportInfo.Remote = local;

            tunnel.OnFail(tunnelTransportInfo);
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

            if (signCaching.Get(tunnelTransportInfo.Remote.MachineName, out SignCacheInfo cache) && signCaching.Get(connection.Name, out SignCacheInfo cache1) && cache.GroupId == cache1.GroupId)
            {
                await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    MessengerId = (ushort)TunnelMessengerIds.Begin,
                    Payload = connection.ReceiveRequestWrap.Payload
                });
                connection.Write(Helper.TrueArray);
            }
        }

        [MessengerId((ushort)TunnelMessengerIds.InfoForward)]
        public async Task InfoForward(IConnection connection)
        {
            TunnelTransportExternalIPRequestInfo request = MemoryPackSerializer.Deserialize<TunnelTransportExternalIPRequestInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.Get(request.RemoteMachineName, out SignCacheInfo cache) && signCaching.Get(connection.Name, out SignCacheInfo cache1) && cache.GroupId == cache1.GroupId)
            {
                MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    MessengerId = (ushort)TunnelMessengerIds.Info,
                    Payload = connection.ReceiveRequestWrap.Payload
                });
                if (resp.Code == MessageResponeCodes.OK)
                {
                    connection.Write(MemoryPackSerializer.Serialize(MemoryPackSerializer.Deserialize<TunnelTransportExternalIPInfo>(resp.Data.Span)));
                }
            }
        }

        [MessengerId((ushort)TunnelMessengerIds.FailForward)]
        public async Task FailForward(IConnection connection)
        {
            TunnelTransportInfo tunnelTransportInfo = MemoryPackSerializer.Deserialize<TunnelTransportInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.Get(tunnelTransportInfo.Remote.MachineName, out SignCacheInfo cache) && signCaching.Get(connection.Name, out SignCacheInfo cache1) && cache.GroupId == cache1.GroupId)
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    MessengerId = (ushort)TunnelMessengerIds.Fail,
                    Payload = connection.ReceiveRequestWrap.Payload
                });
            }
        }
    }
}
