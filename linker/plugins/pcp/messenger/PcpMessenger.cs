using linker.plugins.signin.messenger;
using linker.tunnel;
using linker.tunnel.transport;
using linker.libs;
using MemoryPack;
using linker.plugins.messenger;
using linker.messenger;

namespace linker.plugins.pcp.messenger
{
    public sealed class PcpClientMessenger : IMessenger
    {
        private readonly TunnelTransfer tunnel;
        private readonly IMessengerSender messengerSender;

        public PcpClientMessenger(TunnelTransfer tunnel, IMessengerSender messengerSender)
        {
            this.tunnel = tunnel;
            this.messengerSender = messengerSender;
        }

        [MessengerId((ushort)PcpMessengerIds.Begin)]
        public void Begin(IConnection connection)
        {
            connection.Write(Helper.TrueArray);
        }

      

        [MessengerId((ushort)PcpMessengerIds.Fail)]
        public void Fail(IConnection connection)
        {
        }

        [MessengerId((ushort)PcpMessengerIds.Success)]
        public void Success(IConnection connection)
        {
        }
    }

    public sealed class PcpServerMessenger : IMessenger
    {
        private readonly IMessengerSender messengerSender;
        private readonly SignCaching signCaching;
        public PcpServerMessenger(IMessengerSender messengerSender, SignCaching signCaching)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
        }

        [MessengerId((ushort)PcpMessengerIds.BeginForward)]
        public async Task BeginForward(IConnection connection)
        {
            TunnelTransportInfo tunnelTransportInfo = MemoryPackSerializer.Deserialize<TunnelTransportInfo>(connection.ReceiveRequestWrap.Payload.Span);

            if (signCaching.TryGet(tunnelTransportInfo.Remote.MachineId, out SignCacheInfo cacheTo) && signCaching.TryGet(connection.Id, out SignCacheInfo cacheFrom) && cacheFrom.GroupId == cacheTo.GroupId)
            {
                tunnelTransportInfo.Local.MachineName = cacheFrom.MachineName;
                tunnelTransportInfo.Remote.MachineName = cacheTo.MachineName;

                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = cacheTo.Connection,
                    MessengerId = (ushort)PcpMessengerIds.Begin,
                    Payload = MemoryPackSerializer.Serialize(tunnelTransportInfo)
                }).ConfigureAwait(false);
                connection.Write(Helper.TrueArray);
            }
        }


        [MessengerId((ushort)PcpMessengerIds.FailForward)]
        public async Task FailForward(IConnection connection)
        {
            TunnelTransportInfo tunnelTransportInfo = MemoryPackSerializer.Deserialize<TunnelTransportInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(tunnelTransportInfo.Remote.MachineId, out SignCacheInfo cache) && signCaching.TryGet(connection.Id, out SignCacheInfo cache1) && cache.GroupId == cache1.GroupId)
            {
                tunnelTransportInfo.Local.MachineName = cache1.MachineName;
                tunnelTransportInfo.Remote.MachineName = cache.MachineName;
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    MessengerId = (ushort)PcpMessengerIds.Fail,
                    Payload = MemoryPackSerializer.Serialize(tunnelTransportInfo)
                }).ConfigureAwait(false);
            }
        }


        [MessengerId((ushort)PcpMessengerIds.SuccessForward)]
        public async Task SuccessForward(IConnection connection)
        {
            TunnelTransportInfo tunnelTransportInfo = MemoryPackSerializer.Deserialize<TunnelTransportInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(tunnelTransportInfo.Remote.MachineId, out SignCacheInfo cache) && signCaching.TryGet(connection.Id, out SignCacheInfo cache1) && cache.GroupId == cache1.GroupId)
            {
                tunnelTransportInfo.Local.MachineName = cache1.MachineName;
                tunnelTransportInfo.Remote.MachineName = cache.MachineName;
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    MessengerId = (ushort)PcpMessengerIds.Success,
                    Payload = MemoryPackSerializer.Serialize(tunnelTransportInfo)
                }).ConfigureAwait(false);
            }
        }
    }

}
