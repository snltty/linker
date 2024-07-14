using linker.config;
using linker.plugins.signin.messenger;
using linker.server;
using linker.tunnel;
using linker.tunnel.adapter;
using linker.tunnel.transport;
using linker.libs;
using MemoryPack;

namespace linker.plugins.tunnel.messenger
{
    public sealed class TunnelClientMessenger : IMessenger
    {
        private readonly TunnelTransfer tunnel;
        private readonly TunnelConfigTransfer tunnelConfigTransfer;

        public TunnelClientMessenger(TunnelTransfer tunnel, TunnelConfigTransfer tunnelConfigTransfer)
        {
            this.tunnel = tunnel;
            this.tunnelConfigTransfer = tunnelConfigTransfer;
        }

        [MessengerId((ushort)TunnelMessengerIds.Begin)]
        public void Begin(IConnection connection)
        {
            TunnelTransportInfo tunnelTransportInfo = MemoryPackSerializer.Deserialize<TunnelTransportInfo>(connection.ReceiveRequestWrap.Payload.Span);
            TunnelTransportWanPortInfo local = tunnelTransportInfo.Local;
            tunnelTransportInfo.Local = tunnelTransportInfo.Remote;
            tunnelTransportInfo.Remote = local;

            tunnel.OnBegin(tunnelTransportInfo);
            connection.Write(Helper.TrueArray);
        }

        [MessengerId((ushort)TunnelMessengerIds.Info)]
        public async Task Info(IConnection connection)
        {
            TunnelWanPortProtocolInfo info = MemoryPackSerializer.Deserialize<TunnelWanPortProtocolInfo>(connection.ReceiveRequestWrap.Payload.Span);
            TunnelTransportWanPortInfo tunnelTransportPortInfo = await tunnel.GetWanPort(info).ConfigureAwait(false);
            if (tunnelTransportPortInfo != null)
            {
                connection.Write(MemoryPackSerializer.Serialize(tunnelTransportPortInfo));
            }
        }

        [MessengerId((ushort)TunnelMessengerIds.Fail)]
        public void Fail(IConnection connection)
        {
            TunnelTransportInfo tunnelTransportInfo = MemoryPackSerializer.Deserialize<TunnelTransportInfo>(connection.ReceiveRequestWrap.Payload.Span);
            TunnelTransportWanPortInfo local = tunnelTransportInfo.Local;
            tunnelTransportInfo.Local = tunnelTransportInfo.Remote;
            tunnelTransportInfo.Remote = local;

            tunnel.OnFail(tunnelTransportInfo);
        }

        [MessengerId((ushort)TunnelMessengerIds.Success)]
        public void Success(IConnection connection)
        {
            TunnelTransportInfo tunnelTransportInfo = MemoryPackSerializer.Deserialize<TunnelTransportInfo>(connection.ReceiveRequestWrap.Payload.Span);
            TunnelTransportWanPortInfo local = tunnelTransportInfo.Local;
            tunnelTransportInfo.Local = tunnelTransportInfo.Remote;
            tunnelTransportInfo.Remote = local;

            tunnel.OnSuccess(tunnelTransportInfo);
        }

        [MessengerId((ushort)TunnelMessengerIds.RouteLevel)]
        public void RouteLevel(IConnection connection)
        {
            TunnelTransportRouteLevelInfo tunnelTransportFileConfigInfo = MemoryPackSerializer.Deserialize<TunnelTransportRouteLevelInfo>(connection.ReceiveRequestWrap.Payload.Span);
            tunnelConfigTransfer.OnLocalRouteLevel(tunnelTransportFileConfigInfo);
        }

        [MessengerId((ushort)TunnelMessengerIds.Config)]
        public void Config(IConnection connection)
        {
            TunnelTransportRouteLevelInfo tunnelTransportFileConfigInfo = MemoryPackSerializer.Deserialize<TunnelTransportRouteLevelInfo>(connection.ReceiveRequestWrap.Payload.Span);
            TunnelTransportRouteLevelInfo result = tunnelConfigTransfer.OnRemoteRouteLevel(tunnelTransportFileConfigInfo);
            connection.Write(MemoryPackSerializer.Serialize(result));
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

        [MessengerId((ushort)TunnelMessengerIds.InfoForward)]
        public void InfoForward(IConnection connection)
        {
            TunnelWanPortProtocolInfo info = MemoryPackSerializer.Deserialize<TunnelWanPortProtocolInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(info.MachineId, out SignCacheInfo cache) && signCaching.TryGet(connection.Id, out SignCacheInfo cache1) && cache.GroupId == cache1.GroupId)
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;
                _ = messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    MessengerId = (ushort)TunnelMessengerIds.Info,
                    Payload = connection.ReceiveRequestWrap.Payload,
                }).ContinueWith(async (result) =>
                {
                    if (result.Result.Code == MessageResponeCodes.OK && result.Result.Data.Length > 0)
                    {
                        await messengerSender.ReplyOnly(new MessageResponseWrap
                        {
                            Connection = connection,
                            Payload = MemoryPackSerializer.Serialize(MemoryPackSerializer.Deserialize<TunnelTransportWanPortInfo>(result.Result.Data.Span)),
                            RequestId = requestid,
                        }).ConfigureAwait(false);
                    }
                });
            }
        }


        [MessengerId((ushort)TunnelMessengerIds.BeginForward)]
        public async Task BeginForward(IConnection connection)
        {
            TunnelTransportInfo tunnelTransportInfo = MemoryPackSerializer.Deserialize<TunnelTransportInfo>(connection.ReceiveRequestWrap.Payload.Span);

            if (signCaching.TryGet(tunnelTransportInfo.Remote.MachineId, out SignCacheInfo cache) && signCaching.TryGet(connection.Id, out SignCacheInfo cache1) && cache.GroupId == cache1.GroupId)
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    MessengerId = (ushort)TunnelMessengerIds.Begin,
                    Payload = connection.ReceiveRequestWrap.Payload
                }).ConfigureAwait(false);
                connection.Write(Helper.TrueArray);
            }
        }


        [MessengerId((ushort)TunnelMessengerIds.FailForward)]
        public async Task FailForward(IConnection connection)
        {
            TunnelTransportInfo tunnelTransportInfo = MemoryPackSerializer.Deserialize<TunnelTransportInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(tunnelTransportInfo.Remote.MachineId, out SignCacheInfo cache) && signCaching.TryGet(connection.Id, out SignCacheInfo cache1) && cache.GroupId == cache1.GroupId)
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    MessengerId = (ushort)TunnelMessengerIds.Fail,
                    Payload = connection.ReceiveRequestWrap.Payload
                }).ConfigureAwait(false);
            }
        }


        [MessengerId((ushort)TunnelMessengerIds.SuccessForward)]
        public async Task SuccessForward(IConnection connection)
        {
            TunnelTransportInfo tunnelTransportInfo = MemoryPackSerializer.Deserialize<TunnelTransportInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(tunnelTransportInfo.Remote.MachineId, out SignCacheInfo cache) && signCaching.TryGet(connection.Id, out SignCacheInfo cache1) && cache.GroupId == cache1.GroupId)
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    MessengerId = (ushort)TunnelMessengerIds.Success,
                    Payload = connection.ReceiveRequestWrap.Payload
                }).ConfigureAwait(false);
            }
        }


        [MessengerId((ushort)TunnelMessengerIds.RouteLevelForward)]
        public async Task RouteLevelForward(IConnection connection)
        {
            TunnelTransportRouteLevelInfo tunnelTransportInfo = MemoryPackSerializer.Deserialize<TunnelTransportRouteLevelInfo>(connection.ReceiveRequestWrap.Payload.Span);
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

        [MessengerId((ushort)TunnelMessengerIds.ConfigForward)]
        public void ConfigForward(IConnection connection)
        {
            TunnelTransportRouteLevelInfo tunnelTransportRouteLevelInfo = MemoryPackSerializer.Deserialize<TunnelTransportRouteLevelInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache))
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;

                List<SignCacheInfo> caches = signCaching.Get(cache.GroupId);
                List<Task<MessageResponeInfo>> tasks = new List<Task<MessageResponeInfo>>();
                foreach (SignCacheInfo item in caches.Where(c => c.MachineId != connection.Id && c.Connected))
                {
                    tasks.Add(messengerSender.SendReply(new MessageRequestWrap
                    {
                        Connection = item.Connection,
                        MessengerId = (ushort)TunnelMessengerIds.Config,
                        Timeout = 3000,
                        Payload = connection.ReceiveRequestWrap.Payload
                    }));
                }

                Task.WhenAll(tasks).ContinueWith(async (result) =>
                {
                    List<TunnelTransportRouteLevelInfo> results = tasks.Where(c => c.Result.Code == MessageResponeCodes.OK).Select(c => MemoryPackSerializer.Deserialize<TunnelTransportRouteLevelInfo>(c.Result.Data.Span)).ToList();
                    await messengerSender.ReplyOnly(new MessageResponseWrap
                    {
                        Connection = connection,
                        Payload = MemoryPackSerializer.Serialize(results),
                        RequestId = requestid,
                    }).ConfigureAwait(false);
                });
            }
        }

    }


}
