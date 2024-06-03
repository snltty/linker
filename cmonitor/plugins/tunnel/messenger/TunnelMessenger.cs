using cmonitor.plugins.signin.messenger;
using cmonitor.plugins.tunnel.compact;
using cmonitor.plugins.tunnel.transport;
using cmonitor.server;
using common.libs;
using MemoryPack;

namespace cmonitor.plugins.tunnel.messenger
{
    public sealed class TunnelClientMessenger : IMessenger
    {
        private readonly TunnelTransfer tunnel;
        private readonly TunnelCompactTransfer tunnelCompactTransfer;

        public TunnelClientMessenger(TunnelTransfer tunnel, TunnelCompactTransfer tunnelCompactTransfer)
        {
            this.tunnel = tunnel;
            this.tunnelCompactTransfer = tunnelCompactTransfer;
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

        [MessengerId((ushort)TunnelMessengerIds.Success)]
        public void Success(IConnection connection)
        {
            TunnelTransportInfo tunnelTransportInfo = MemoryPackSerializer.Deserialize<TunnelTransportInfo>(connection.ReceiveRequestWrap.Payload.Span);
            TunnelTransportExternalIPInfo local = tunnelTransportInfo.Local;
            tunnelTransportInfo.Local = tunnelTransportInfo.Remote;
            tunnelTransportInfo.Remote = local;

            tunnel.OnSuccess(tunnelTransportInfo);
        }

        [MessengerId((ushort)TunnelMessengerIds.RouteLevel)]
        public void RouteLevel(IConnection connection)
        {
            TunnelTransportRouteLevelInfo tunnelTransportConfigWrapInfo = MemoryPackSerializer.Deserialize<TunnelTransportRouteLevelInfo>(connection.ReceiveRequestWrap.Payload.Span);
            tunnel.OnLocalRouteLevel(tunnelTransportConfigWrapInfo);
        }

        [MessengerId((ushort)TunnelMessengerIds.Config)]
        public void Config(IConnection connection)
        {
            TunnelTransportRouteLevelInfo tunnelTransportConfigWrapInfo = MemoryPackSerializer.Deserialize<TunnelTransportRouteLevelInfo>(connection.ReceiveRequestWrap.Payload.Span);
            TunnelTransportRouteLevelInfo result = tunnel.OnRemoteRouteLevel(tunnelTransportConfigWrapInfo);
            connection.Write(MemoryPackSerializer.Serialize(result));
        }


        [MessengerId((ushort)TunnelMessengerIds.Transport)]
        public void Transport(IConnection connection)
        {
            List<TunnelTransportItemInfo> transports = MemoryPackSerializer.Deserialize<List<TunnelTransportItemInfo>>(connection.ReceiveRequestWrap.Payload.Span);
            tunnel.OnRemoteTransports(transports);
        }

        [MessengerId((ushort)TunnelMessengerIds.Servers)]
        public void Servers(IConnection connection)
        {
            TunnelCompactInfo[] servers = MemoryPackSerializer.Deserialize<TunnelCompactInfo[]>(connection.ReceiveRequestWrap.Payload.Span);
            tunnelCompactTransfer.OnServers(servers);
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
            TunnelTransportExternalIPRequestInfo request = MemoryPackSerializer.Deserialize<TunnelTransportExternalIPRequestInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.Get(request.RemoteMachineName, out SignCacheInfo cache) && signCaching.Get(connection.Name, out SignCacheInfo cache1) && cache.GroupId == cache1.GroupId)
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
                            Payload = MemoryPackSerializer.Serialize(MemoryPackSerializer.Deserialize<TunnelTransportExternalIPInfo>(result.Result.Data.Span)),
                            RequestId = requestid,
                        });
                    }
                });
            }
        }


        [MessengerId((ushort)TunnelMessengerIds.BeginForward)]
        public async Task BeginForward(IConnection connection)
        {
            TunnelTransportInfo tunnelTransportInfo = MemoryPackSerializer.Deserialize<TunnelTransportInfo>(connection.ReceiveRequestWrap.Payload.Span);

            if (signCaching.Get(tunnelTransportInfo.Remote.MachineName, out SignCacheInfo cache) && signCaching.Get(connection.Name, out SignCacheInfo cache1) && cache.GroupId == cache1.GroupId)
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    MessengerId = (ushort)TunnelMessengerIds.Begin,
                    Payload = connection.ReceiveRequestWrap.Payload
                });
                connection.Write(Helper.TrueArray);
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


        [MessengerId((ushort)TunnelMessengerIds.SuccessForward)]
        public async Task SuccessForward(IConnection connection)
        {
            TunnelTransportInfo tunnelTransportInfo = MemoryPackSerializer.Deserialize<TunnelTransportInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.Get(tunnelTransportInfo.Remote.MachineName, out SignCacheInfo cache) && signCaching.Get(connection.Name, out SignCacheInfo cache1) && cache.GroupId == cache1.GroupId)
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    MessengerId = (ushort)TunnelMessengerIds.Success,
                    Payload = connection.ReceiveRequestWrap.Payload
                });
            }
        }


        [MessengerId((ushort)TunnelMessengerIds.RouteLevelForward)]
        public async Task RouteLevelForward(IConnection connection)
        {
            TunnelTransportRouteLevelInfo tunnelTransportInfo = MemoryPackSerializer.Deserialize<TunnelTransportRouteLevelInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.Get(tunnelTransportInfo.MachineName, out SignCacheInfo cache) && signCaching.Get(connection.Name, out SignCacheInfo cache1) && cache.GroupId == cache1.GroupId)
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    MessengerId = (ushort)TunnelMessengerIds.RouteLevel,
                    Payload = connection.ReceiveRequestWrap.Payload
                });
            }

        }

        [MessengerId((ushort)TunnelMessengerIds.ConfigForward)]
        public void ConfigForward(IConnection connection)
        {
            if (signCaching.Get(connection.Name, out SignCacheInfo cache))
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;

                List<SignCacheInfo> caches = signCaching.Get(cache.GroupId);
                List<Task<MessageResponeInfo>> tasks = new List<Task<MessageResponeInfo>>();
                foreach (SignCacheInfo item in caches.Where(c => c.MachineName != connection.Name && c.Connected))
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
                    });
                });
            }
        }


        [MessengerId((ushort)TunnelMessengerIds.TransportForward)]
        public async Task TransportForward(IConnection connection)
        {
            if (signCaching.Get(connection.Name, out SignCacheInfo cache))
            {
                List<SignCacheInfo> caches = signCaching.Get(cache.GroupId);

                foreach (SignCacheInfo item in caches.Where(c => c.MachineName != connection.Name && c.Connected))
                {
                    await messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = item.Connection,
                        MessengerId = (ushort)TunnelMessengerIds.Transport,
                        Payload = connection.ReceiveRequestWrap.Payload
                    });
                }
            }
        }

        [MessengerId((ushort)TunnelMessengerIds.ServersForward)]
        public async Task ServersForward(IConnection connection)
        {
            if (signCaching.Get(connection.Name, out SignCacheInfo cache))
            {
                List<SignCacheInfo> caches = signCaching.Get(cache.GroupId);

                foreach (SignCacheInfo item in caches.Where(c => c.MachineName != connection.Name && c.Connected))
                {
                    await messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = item.Connection,
                        MessengerId = (ushort)TunnelMessengerIds.Servers,
                        Payload = connection.ReceiveRequestWrap.Payload
                    });
                }
            }
        }
    }


}
