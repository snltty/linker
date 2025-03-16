using linker.tunnel;
using linker.tunnel.transport;
using linker.libs;
using linker.messenger.signin;
using linker.plugins.tunnel;

namespace linker.messenger.tunnel
{
    /// <summary>
    /// 打洞信标客户端
    /// </summary>
    public class TunnelClientMessenger : IMessenger
    {
        private readonly TunnelTransfer tunnel;
        private readonly IMessengerSender messengerSender;
        private readonly ISerializer serializer;
        private readonly ITunnelClientStore tunnelClientStore;

        public TunnelClientMessenger(TunnelTransfer tunnel, IMessengerSender messengerSender, ISerializer serializer, ITunnelClientStore tunnelClientStore)
        {
            this.tunnel = tunnel;
            this.messengerSender = messengerSender;
            this.serializer = serializer;
            this.tunnelClientStore = tunnelClientStore;
        }

        [MessengerId((ushort)TunnelMessengerIds.Begin)]
        public void Begin(IConnection connection)
        {
            TunnelTransportInfo tunnelTransportInfo = serializer.Deserialize<TunnelTransportInfo>(connection.ReceiveRequestWrap.Payload.Span);
            TunnelTransportWanPortInfo local = tunnelTransportInfo.Local;
            tunnelTransportInfo.Local = tunnelTransportInfo.Remote;
            tunnelTransportInfo.Remote = local;

            _ = tunnel.OnBegin(tunnelTransportInfo);
            connection.Write(Helper.TrueArray);
        }

        [MessengerId((ushort)TunnelMessengerIds.Info)]
        public void Info(IConnection connection)
        {
            TunnelWanPortProtocolInfo info = serializer.Deserialize<TunnelWanPortProtocolInfo>(connection.ReceiveRequestWrap.Payload.Span);

            uint requestid = connection.ReceiveRequestWrap.RequestId;
            tunnel.GetWanPort(info).ContinueWith(async (result) =>
            {
                if (result.Result == null)
                {
                    await messengerSender.ReplyOnly(new MessageResponseWrap
                    {
                        Connection = connection,
                        Code = MessageResponeCodes.ERROR,
                        Payload = Helper.EmptyArray,
                        RequestId = requestid
                    }, (ushort)TunnelMessengerIds.Info).ConfigureAwait(false);
                }
                else
                {
                    await messengerSender.ReplyOnly(new MessageResponseWrap
                    {
                        Connection = connection,
                        Code = MessageResponeCodes.OK,
                        Payload = serializer.Serialize(result.Result),
                        RequestId = requestid
                    }, (ushort)TunnelMessengerIds.Info).ConfigureAwait(false);
                }
            });
        }

        [MessengerId((ushort)TunnelMessengerIds.Fail)]
        public void Fail(IConnection connection)
        {
            TunnelTransportInfo tunnelTransportInfo = serializer.Deserialize<TunnelTransportInfo>(connection.ReceiveRequestWrap.Payload.Span);
            TunnelTransportWanPortInfo local = tunnelTransportInfo.Local;
            tunnelTransportInfo.Local = tunnelTransportInfo.Remote;
            tunnelTransportInfo.Remote = local;

            tunnel.OnFail(tunnelTransportInfo);
        }

        [MessengerId((ushort)TunnelMessengerIds.Success)]
        public void Success(IConnection connection)
        {
            TunnelTransportInfo tunnelTransportInfo = serializer.Deserialize<TunnelTransportInfo>(connection.ReceiveRequestWrap.Payload.Span);
            TunnelTransportWanPortInfo local = tunnelTransportInfo.Local;
            tunnelTransportInfo.Local = tunnelTransportInfo.Remote;
            tunnelTransportInfo.Remote = local;

            tunnel.OnSuccess(tunnelTransportInfo);
        }


        [MessengerId((ushort)TunnelMessengerIds.RouteLevel)]
        public async Task RouteLevel(IConnection connection)
        {
            TunnelSetRouteLevelInfo tunnelTransportFileConfigInfo = serializer.Deserialize<TunnelSetRouteLevelInfo>(connection.ReceiveRequestWrap.Payload.Span);
            await tunnelClientStore.SetRouteLevelPlus(tunnelTransportFileConfigInfo.RouteLevelPlus).ConfigureAwait(false);
            await tunnelClientStore.SetPortMap(tunnelTransportFileConfigInfo.PortMapLan, tunnelTransportFileConfigInfo.PortMapWan).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 打洞信标服务端
    /// </summary>
    public class TunnelServerMessenger : IMessenger
    {
        private readonly IMessengerSender messengerSender;
        private readonly SignInServerCaching signCaching;
        private readonly ISerializer serializer;

        public TunnelServerMessenger(IMessengerSender messengerSender, SignInServerCaching signCaching, ISerializer serializer)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
            this.serializer = serializer;
        }

        [MessengerId((ushort)TunnelMessengerIds.InfoForward)]
        public void InfoForward(IConnection connection)
        {
            TunnelWanPortProtocolInfo info = serializer.Deserialize<TunnelWanPortProtocolInfo>(connection.ReceiveRequestWrap.Payload.Span);
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
                            Payload = serializer.Serialize(serializer.Deserialize<TunnelTransportWanPortInfo>(result.Result.Data.Span)),
                            RequestId = requestid,
                        }, (ushort)TunnelMessengerIds.InfoForward).ConfigureAwait(false);
                    }
                });
            }
        }


        [MessengerId((ushort)TunnelMessengerIds.BeginForward)]
        public async Task BeginForward(IConnection connection)
        {
            TunnelTransportInfo tunnelTransportInfo = serializer.Deserialize<TunnelTransportInfo>(connection.ReceiveRequestWrap.Payload.Span);

            if (signCaching.TryGet(tunnelTransportInfo.Remote.MachineId, out SignCacheInfo cacheTo) && signCaching.TryGet(connection.Id, out SignCacheInfo cacheFrom) && cacheFrom.GroupId == cacheTo.GroupId)
            {
                tunnelTransportInfo.Local.MachineName = cacheFrom.MachineName;
                tunnelTransportInfo.Remote.MachineName = cacheTo.MachineName;

                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = cacheTo.Connection,
                    MessengerId = (ushort)TunnelMessengerIds.Begin,
                    Payload = serializer.Serialize(tunnelTransportInfo)
                }).ConfigureAwait(false);
                connection.Write(Helper.TrueArray);
            }
        }


        [MessengerId((ushort)TunnelMessengerIds.FailForward)]
        public async Task FailForward(IConnection connection)
        {
            TunnelTransportInfo tunnelTransportInfo = serializer.Deserialize<TunnelTransportInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(tunnelTransportInfo.Remote.MachineId, out SignCacheInfo cache) && signCaching.TryGet(connection.Id, out SignCacheInfo cache1) && cache.GroupId == cache1.GroupId)
            {
                tunnelTransportInfo.Local.MachineName = cache1.MachineName;
                tunnelTransportInfo.Remote.MachineName = cache.MachineName;
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    MessengerId = (ushort)TunnelMessengerIds.Fail,
                    Payload = serializer.Serialize(tunnelTransportInfo)
                }).ConfigureAwait(false);
            }
        }


        [MessengerId((ushort)TunnelMessengerIds.SuccessForward)]
        public async Task SuccessForward(IConnection connection)
        {
            TunnelTransportInfo tunnelTransportInfo = serializer.Deserialize<TunnelTransportInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(tunnelTransportInfo.Remote.MachineId, out SignCacheInfo cache) && signCaching.TryGet(connection.Id, out SignCacheInfo cache1) && cache.GroupId == cache1.GroupId)
            {
                tunnelTransportInfo.Local.MachineName = cache1.MachineName;
                tunnelTransportInfo.Remote.MachineName = cache.MachineName;
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    MessengerId = (ushort)TunnelMessengerIds.Success,
                    Payload = serializer.Serialize(tunnelTransportInfo)
                }).ConfigureAwait(false);
            }
        }


        [MessengerId((ushort)TunnelMessengerIds.RouteLevelForward)]
        public async Task RouteLevelForward(IConnection connection)
        {
            TunnelRouteLevelInfo tunnelTransportInfo = serializer.Deserialize<TunnelRouteLevelInfo>(connection.ReceiveRequestWrap.Payload.Span);
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
