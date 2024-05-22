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

        [MessengerId((ushort)TunnelMessengerIds.Success)]
        public void Success(IConnection connection)
        {
            TunnelTransportInfo tunnelTransportInfo = MemoryPackSerializer.Deserialize<TunnelTransportInfo>(connection.ReceiveRequestWrap.Payload.Span);
            TunnelTransportExternalIPInfo local = tunnelTransportInfo.Local;
            tunnelTransportInfo.Local = tunnelTransportInfo.Remote;
            tunnelTransportInfo.Remote = local;

            tunnel.OnSuccess(tunnelTransportInfo);
        }


        [MessengerId((ushort)TunnelMessengerIds.Update)]
        public void Update(IConnection connection)
        {
            TunnelTransportConfigInfo tunnelTransportConfigWrapInfo = MemoryPackSerializer.Deserialize<TunnelTransportConfigInfo>(connection.ReceiveRequestWrap.Payload.Span);
            tunnel.OnUpdate(tunnelTransportConfigWrapInfo);
        }


        [MessengerId((ushort)TunnelMessengerIds.Config)]
        public void Config(IConnection connection)
        {
            TunnelTransportConfigInfo tunnelTransportConfigWrapInfo = MemoryPackSerializer.Deserialize<TunnelTransportConfigInfo>(connection.ReceiveRequestWrap.Payload.Span);
            TunnelTransportConfigInfo result = tunnel.OnConfig(tunnelTransportConfigWrapInfo);
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
                if (resp.Code == MessageResponeCodes.OK && resp.Data.Span.Length > 0)
                {
                    connection.Write(MemoryPackSerializer.Serialize(MemoryPackSerializer.Deserialize<TunnelTransportExternalIPInfo>(resp.Data.Span)));
                }
            }
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


        [MessengerId((ushort)TunnelMessengerIds.UpdateForward)]
        public async Task UpdateForward(IConnection connection)
        {
            TunnelTransportConfigInfo tunnelTransportInfo = MemoryPackSerializer.Deserialize<TunnelTransportConfigInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.Get(tunnelTransportInfo.MachineName, out SignCacheInfo cache) && signCaching.Get(connection.Name, out SignCacheInfo cache1) && cache.GroupId == cache1.GroupId)
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    MessengerId = (ushort)TunnelMessengerIds.Update,
                    Payload = connection.ReceiveRequestWrap.Payload
                });
            }

        }

        [MessengerId((ushort)TunnelMessengerIds.ConfigForward)]
        public async Task ConfigForward(IConnection connection)
        {
            if (signCaching.Get(connection.Name, out SignCacheInfo cache))
            {
                List<SignCacheInfo> caches = signCaching.Get(cache.GroupId);

                List<Task<MessageResponeInfo>> tasks = new List<Task<MessageResponeInfo>>();
                foreach (SignCacheInfo item in caches.Where(c => c.MachineName != connection.Name && c.Connected))
                {
                    tasks.Add(messengerSender.SendReply(new MessageRequestWrap
                    {
                        Connection = item.Connection,
                        MessengerId = (ushort)TunnelMessengerIds.Config,
                        Payload = connection.ReceiveRequestWrap.Payload
                    }));
                }

                await Task.WhenAll(tasks);

                List<TunnelTransportConfigInfo> results = tasks.Where(c => c.Result.Code == MessageResponeCodes.OK).Select(c => MemoryPackSerializer.Deserialize<TunnelTransportConfigInfo>(c.Result.Data.Span)).ToList();
                connection.Write(MemoryPackSerializer.Serialize(results));
            }
        }
    }


}
