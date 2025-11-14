using linker.tunnel;
using linker.tunnel.transport;
using linker.libs;
using linker.messenger.signin;

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
        private readonly TunnelNetworkTransfer tunnelNetworkTransfer;
        private readonly ITunnelMessengerAdapter tunnelMessengerAdapter;

        public TunnelClientMessenger(TunnelTransfer tunnel, IMessengerSender messengerSender,
            ISerializer serializer, ITunnelClientStore tunnelClientStore,
            TunnelNetworkTransfer tunnelNetworkTransfer, ITunnelMessengerAdapter tunnelMessengerAdapter)
        {
            this.tunnel = tunnel;
            this.messengerSender = messengerSender;
            this.serializer = serializer;
            this.tunnelClientStore = tunnelClientStore;
            this.tunnelNetworkTransfer = tunnelNetworkTransfer;
            this.tunnelMessengerAdapter = tunnelMessengerAdapter;
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

        [MessengerId((ushort)TunnelMessengerIds.Network)]
        public void Network(IConnection connection)
        {
            connection.Write(serializer.Serialize(tunnelNetworkTransfer.GetLocalNetwork()));
        }

        [MessengerId((ushort)TunnelMessengerIds.TransportGet)]
        public async Task TransportGet(IConnection connection)
        {
            string machineid = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            connection.Write(serializer.Serialize(await tunnelMessengerAdapter.GetTunnelTransports(machineid)));
        }
        [MessengerId((ushort)TunnelMessengerIds.TransportSet)]
        public async Task TransportSet(IConnection connection)
        {
            TunnelTransportItemSetInfo info = serializer.Deserialize<TunnelTransportItemSetInfo>(connection.ReceiveRequestWrap.Payload.Span);
            await tunnelMessengerAdapter.SetTunnelTransports(info.MachineId, info.Data);
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
            if (signCaching.TryGet(connection.Id, info.MachineId, out SignCacheInfo from, out SignCacheInfo to))
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;
                _ = messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = to.Connection,
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
            TunnelTransportInfo info = serializer.Deserialize<TunnelTransportInfo>(connection.ReceiveRequestWrap.Payload.Span);

            if (signCaching.TryGet(connection.Id, info.Remote.MachineId, out SignCacheInfo from, out SignCacheInfo to))
            {
                info.Local.MachineName = from.MachineName;
                info.Remote.MachineName = to.MachineName;

                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)TunnelMessengerIds.Begin,
                    Payload = serializer.Serialize(info)
                }).ConfigureAwait(false);
                connection.Write(Helper.TrueArray);
            }
        }


        [MessengerId((ushort)TunnelMessengerIds.FailForward)]
        public async Task FailForward(IConnection connection)
        {
            TunnelTransportInfo info = serializer.Deserialize<TunnelTransportInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, info.Remote.MachineId, out SignCacheInfo from, out SignCacheInfo to))
            {
                info.Local.MachineName = from.MachineName;
                info.Remote.MachineName = to.MachineName;
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)TunnelMessengerIds.Fail,
                    Payload = serializer.Serialize(info)
                }).ConfigureAwait(false);
            }
        }


        [MessengerId((ushort)TunnelMessengerIds.SuccessForward)]
        public async Task SuccessForward(IConnection connection)
        {
            TunnelTransportInfo info = serializer.Deserialize<TunnelTransportInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, info.Remote.MachineId, out SignCacheInfo from, out SignCacheInfo to))
            {
                info.Local.MachineName = from.MachineName;
                info.Remote.MachineName = to.MachineName;
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)TunnelMessengerIds.Success,
                    Payload = serializer.Serialize(info)
                }).ConfigureAwait(false);
            }
        }


        [MessengerId((ushort)TunnelMessengerIds.RouteLevelForward)]
        public async Task RouteLevelForward(IConnection connection)
        {
            TunnelSetRouteLevelInfo info = serializer.Deserialize<TunnelSetRouteLevelInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, info.MachineId, out SignCacheInfo from, out SignCacheInfo to))
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)TunnelMessengerIds.RouteLevel,
                    Payload = connection.ReceiveRequestWrap.Payload
                }).ConfigureAwait(false);
            }
        }

        [MessengerId((ushort)TunnelMessengerIds.NetworkForward)]
        public void NetworkForward(IConnection connection)
        {
            string machineid = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, machineid, out SignCacheInfo from, out SignCacheInfo to))
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;
                _ = messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)TunnelMessengerIds.Network
                }).ContinueWith(async (result) =>
                {
                    if (result.Result.Code == MessageResponeCodes.OK && result.Result.Data.Length > 0)
                    {
                        await messengerSender.ReplyOnly(new MessageResponseWrap
                        {
                            Connection = connection,
                            Payload = serializer.Serialize(serializer.Deserialize<TunnelLocalNetworkInfo>(result.Result.Data.Span)),
                            RequestId = requestid,
                        }, (ushort)TunnelMessengerIds.NetworkForward).ConfigureAwait(false);
                    }
                });
            }
        }


        [MessengerId((ushort)TunnelMessengerIds.TransportGetForward)]
        public void TransportGetForward(IConnection connection)
        {
            string machineid = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, machineid, out SignCacheInfo from, out SignCacheInfo to))
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;
                _ = messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)TunnelMessengerIds.TransportGet,
                     Payload = serializer.Serialize(connection.Id)
                }).ContinueWith(async (result) =>
                {
                    if (result.Result.Code == MessageResponeCodes.OK && result.Result.Data.Length > 0)
                    {
                        await messengerSender.ReplyOnly(new MessageResponseWrap
                        {
                            Connection = connection,
                            Payload = result.Result.Data,
                            RequestId = requestid,
                        }, (ushort)TunnelMessengerIds.TransportGetForward).ConfigureAwait(false);
                    }
                });
            }
        }
        [MessengerId((ushort)TunnelMessengerIds.TransportSetForward)]
        public void TransportSetForward(IConnection connection)
        {
            TunnelTransportItemSetInfo info = serializer.Deserialize<TunnelTransportItemSetInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, info.MachineId, out SignCacheInfo from, out SignCacheInfo to))
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;
                info.MachineId = connection.Id;
                _ = messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)TunnelMessengerIds.TransportSet,
                    Payload = serializer.Serialize(info)
                }).ContinueWith(async (result) =>
                {
                    if (result.Result.Code == MessageResponeCodes.OK && result.Result.Data.Length > 0)
                    {
                        await messengerSender.ReplyOnly(new MessageResponseWrap
                        {
                            Connection = connection,
                            Payload = result.Result.Data,
                            RequestId = requestid,
                        }, (ushort)TunnelMessengerIds.TransportSetForward).ConfigureAwait(false);
                    }
                });
            }
        }
    }
}
