using cmonitor.config;
using cmonitor.plugins.relay.transport;
using cmonitor.plugins.signin.messenger;
using cmonitor.server;
using common.libs;
using MemoryPack;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;

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
    }

    public sealed class RelayServerMessenger : IMessenger
    {
        private readonly Config config;
        private readonly MessengerSender messengerSender;
        private readonly SignCaching signCaching;
        private readonly ConcurrentDictionary<ulong, TaskCompletionSource<IConnection>> dic = new ConcurrentDictionary<ulong, TaskCompletionSource<IConnection>>();
        private ulong flowingId = 0;
        

        public RelayServerMessenger(Config config, MessengerSender messengerSender, SignCaching signCaching)
        {
            this.config = config;
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
        }

        [MessengerId((ushort)RelayMessengerIds.RelayForward)]
        public async Task RelayForward(IConnection connection)
        {
            RelayInfo info = MemoryPackSerializer.Deserialize<RelayInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (info.FlowingId == 0)
            {
                if (info.SecretKey != config.Data.Client.Relay.SecretKey)
                {
                    connection.Write(Helper.FalseArray);
                    return;
                }
                if (signCaching.Get(info.RemoteMachineName, out SignCacheInfo cache) == false)
                {
                    connection.Write(Helper.FalseArray);
                    return;
                }

                info.FlowingId = Interlocked.Increment(ref flowingId);
                TaskCompletionSource<IConnection> tcs = new TaskCompletionSource<IConnection>();
                dic.TryAdd(info.FlowingId, tcs);

                try
                {
                    MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
                    {
                        Connection = cache.Connection,
                        MessengerId = (ushort)RelayMessengerIds.Relay,
                        Payload = MemoryPackSerializer.Serialize(info)
                    });
                    if (resp.Code != MessageResponeCodes.OK || resp.Data.Span.SequenceEqual(Helper.TrueArray) == false)
                    {
                        connection.Write(Helper.FalseArray);
                        return;
                    }

                    IConnection targetConnection = await tcs.Task.WaitAsync(TimeSpan.FromMilliseconds(3000));
                    connection.TcpTargetSocket = targetConnection.TcpSourceSocket;
                    targetConnection.TcpTargetSocket = connection.TcpSourceSocket;
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
                if (dic.TryRemove(info.FlowingId, out TaskCompletionSource<IConnection> tcs))
                {
                    tcs.SetResult(connection);
                    connection.Write(Helper.TrueArray);
                }
                else
                {
                    connection.Write(Helper.FalseArray);
                }
                return;
            }
        }
    }


}
