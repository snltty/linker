using cmonitor.config;
using cmonitor.plugins.relay.transport;
using cmonitor.plugins.signin.messenger;
using cmonitor.server;
using common.libs;
using MemoryPack;
using System.Collections.Concurrent;

namespace cmonitor.plugins.relay.messenger
{
    public sealed class RelayClientMessenger : IMessenger
    {
        private readonly RelayTransfer relayTransfer;
        public RelayClientMessenger(RelayTransfer relayTransfer)
        {
            this.relayTransfer = relayTransfer;
        }

        [MessengerId((ushort)RelayMessengerIds.Relay)]
        public async Task Relay(IConnection connection)
        {
            RelayInfo info = MemoryPackSerializer.Deserialize<RelayInfo>(connection.ReceiveRequestWrap.Payload.Span);
            bool res = await relayTransfer.OnBeginAsync(info);
            connection.Write(res ? Helper.TrueArray : Helper.FalseArray);
        }

        [MessengerId((ushort)RelayMessengerIds.Servers)]
        public void Servers(IConnection connection)
        {
            RelayCompactInfo[] servers = MemoryPackSerializer.Deserialize<RelayCompactInfo[]>(connection.ReceiveRequestWrap.Payload.Span);
            relayTransfer.OnServers(servers);
        }
    }

    public sealed class RelayServerMessenger : IMessenger
    {
        private readonly Config config;
        private readonly MessengerSender messengerSender;
        private readonly SignCaching signCaching;
        private readonly ConcurrentDictionary<ulong, TcsWrap> dic = new ConcurrentDictionary<ulong, TcsWrap>();
        private ulong flowingId = 0;


        public RelayServerMessenger(Config config, MessengerSender messengerSender, SignCaching signCaching)
        {
            this.config = config;
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
        }

        [MessengerId((ushort)RelayMessengerIds.ServersForward)]
        public async Task ServersForward(IConnection connection)
        {
            RelayCompactInfo[] servers = MemoryPackSerializer.Deserialize<RelayCompactInfo[]>(connection.ReceiveRequestWrap.Payload.Span);

            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache))
            {
                List<SignCacheInfo> caches = signCaching.Get(cache.GroupId);

                foreach (SignCacheInfo item in caches.Where(c => c.MachineId != connection.Id && c.Connected))
                {
                    await messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = item.Connection,
                        MessengerId = (ushort)RelayMessengerIds.Servers,
                        Payload = connection.ReceiveRequestWrap.Payload
                    });
                }
            }
        }


        [MessengerId((ushort)RelayMessengerIds.RelayForward)]
        public async Task RelayForward(IConnection connection)
        {
            RelayInfo info = MemoryPackSerializer.Deserialize<RelayInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (info.FlowingId == 0)
            {
                if (info.SecretKey != config.Data.Server.Relay.SecretKey)
                {
                    connection.Write(Helper.FalseArray);
                    return;
                }
                if (signCaching.TryGet(info.FromMachineId, out SignCacheInfo cacheFrom) == false || signCaching.TryGet(info.RemoteMachineId, out SignCacheInfo cacheTo) == false)
                {
                    connection.Write(Helper.FalseArray);
                    return;
                }

                info.FlowingId = Interlocked.Increment(ref flowingId);
                info.RemoteMachineId = info.FromMachineId;
                info.FromMachineId = info.RemoteMachineId;
                info.RemoteMachineName = cacheFrom.MachineName;
                info.FromMachineName = cacheTo.MachineName;

                TcsWrap tcsWrap = new TcsWrap { Connection = connection, Tcs = new TaskCompletionSource<IConnection>() };
                dic.TryAdd(info.FlowingId, tcsWrap);

                try
                {
                    bool res = await messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = cacheTo.Connection,
                        MessengerId = (ushort)RelayMessengerIds.Relay,
                        Payload = MemoryPackSerializer.Serialize(info)
                    });
                    if (res == false)
                    {
                        connection.Write(Helper.FalseArray);
                        return;
                    }

                    IConnection targetConnection = await tcsWrap.Tcs.Task.WaitAsync(TimeSpan.FromMilliseconds(3000));

                    _ = Relay(connection, targetConnection, info.SecretKey);

                    connection.Write(Helper.TrueArray);
                }
                catch (Exception)
                {
                    connection.Write(Helper.FalseArray);
                }
                finally
                {
                    dic.TryRemove(info.FlowingId, out _);
                }
            }
            else
            {
                if (dic.TryRemove(info.FlowingId, out TcsWrap tcsWrap))
                {
                    tcsWrap.Tcs.SetResult(connection);
                    connection.Write(Helper.TrueArray);
                }
                else
                {
                    connection.Write(Helper.FalseArray);
                }
                return;
            }
        }

        private async Task Relay(IConnection source, IConnection target, string secretKey)
        {
            await Task.Delay(100);
            int limit = 0;
            source.TargetStream = target.SourceStream;
            source.TargetSocket = target.SourceSocket;
            source.TargetNetworkStream = target.SourceNetworkStream;
            source.RelayLimit = (uint)limit;
            target.TargetStream = source.SourceStream;
            target.TargetSocket = source.SourceSocket;
            target.TargetNetworkStream = source.SourceNetworkStream;
            target.RelayLimit = (uint)limit;

            source.Cancel();
            target.Cancel();

            await Task.Delay(200);

            await Task.WhenAll(source.RelayAsync(), target.RelayAsync());
        }

        public sealed class TcsWrap
        {
            public TaskCompletionSource<IConnection> Tcs { get; set; }
            public IConnection Connection { get; set; }
        }
    }


}
