using linker.config;
using linker.plugins.relay.transport;
using linker.plugins.signin.messenger;
using linker.libs;
using MemoryPack;
using System.Collections.Concurrent;
using linker.plugins.messenger;
using System.Text.Json.Serialization;

namespace linker.plugins.relay.messenger
{
    /// <summary>
    /// 中继客户端
    /// </summary>
    public sealed class RelayClientMessenger : IMessenger
    {
        private readonly RelayTransfer relayTransfer;
        public RelayClientMessenger(RelayTransfer relayTransfer)
        {
            this.relayTransfer = relayTransfer;
        }

        /// <summary>
        /// 收到中继请求
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)RelayMessengerIds.Relay)]
        public async Task Relay(IConnection connection)
        {
            RelayInfo info = MemoryPackSerializer.Deserialize<RelayInfo>(connection.ReceiveRequestWrap.Payload.Span);
            bool res = await relayTransfer.OnBeginAsync(info).ConfigureAwait(false);
            connection.Write(res ? Helper.TrueArray : Helper.FalseArray);
        }

    }

    /// <summary>
    /// 中继服务端
    /// </summary>
    public sealed class RelayServerMessenger : IMessenger
    {
        private readonly FileConfig config;
        private readonly MessengerSender messengerSender;
        private readonly SignCaching signCaching;
        private readonly ConcurrentDictionary<ulong, TcsWrap> dic = new ConcurrentDictionary<ulong, TcsWrap>();
        private ulong flowingId = 0;


        public RelayServerMessenger(FileConfig config, MessengerSender messengerSender, SignCaching signCaching)
        {
            this.config = config;
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
        }

        /// <summary>
        /// 测试一下中继通不通
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)RelayMessengerIds.RelayTest)]
        public void RelayTest(IConnection connection)
        {
            RelayTestInfo info = MemoryPackSerializer.Deserialize<RelayTestInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (info.SecretKey != config.Data.Server.Relay.SecretKey)
            {
                connection.Write(Helper.FalseArray);
                return;
            }
            connection.Write(Helper.TrueArray);
        }


        /// <summary>
        /// 请求中继
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)RelayMessengerIds.RelayAsk)]
        public void RelayAsk(IConnection connection)
        {
            RelayInfo info = MemoryPackSerializer.Deserialize<RelayInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (info.SecretKey != config.Data.Server.Relay.SecretKey)
            {
                connection.Write(ulong.MinValue);
                return;
            }

            info.FlowingId = Interlocked.Increment(ref flowingId);
            _ = WaitConfirm(connection, info);

            connection.Write(info.FlowingId);
        }
        private async Task WaitConfirm(IConnection connection, RelayInfo info)
        {
            try
            {
                TcsWrap tcsWrap = new TcsWrap { Connection = connection, Tcs = new TaskCompletionSource<IConnection>(TaskCreationOptions.RunContinuationsAsynchronously) };
                dic.TryAdd(info.FlowingId, tcsWrap);
                IConnection targetConnection = await tcsWrap.Tcs.Task.WaitAsync(TimeSpan.FromMilliseconds(3000)).ConfigureAwait(false);
                _ = Relay(connection, targetConnection, info.SecretKey);
            }
            catch (Exception)
            {
            }
            finally
            {
                dic.TryRemove(info.FlowingId, out _);
            }
        }

        /// <summary>
        /// 回复，确认中继
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)RelayMessengerIds.RelayConfirm)]
        public void RelayConfirm(IConnection connection)
        {
            RelayInfo info = MemoryPackSerializer.Deserialize<RelayInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (info.SecretKey != config.Data.Server.Relay.SecretKey)
            {
                connection.Write(Helper.FalseArray);
                return;
            }

            if (dic.TryRemove(info.FlowingId, out TcsWrap tcsWrap))
            {
                tcsWrap.Tcs.SetResult(connection);
                connection.Write(Helper.TrueArray);
            }
            else
            {
                connection.Write(Helper.FalseArray);
            }
        }

        /// <summary>
        /// 收到中继请求
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)RelayMessengerIds.RelayForward)]
        public async Task RelayForward(IConnection connection)
        {
            RelayInfo info = MemoryPackSerializer.Deserialize<RelayInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(info.FromMachineId, out SignCacheInfo cacheFrom) == false || signCaching.TryGet(info.RemoteMachineId, out SignCacheInfo cacheTo) == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }
            if (cacheFrom.GroupId != cacheTo.GroupId)
            {
                connection.Write(Helper.FalseArray);
                return;
            }
            info.RemoteMachineId = cacheFrom.MachineId;
            info.FromMachineId = cacheTo.MachineId;
            info.RemoteMachineName = cacheFrom.MachineName;
            info.FromMachineName = cacheTo.MachineName;

            try
            {
                MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = cacheTo.Connection,
                    MessengerId = (ushort)RelayMessengerIds.Relay,
                    Payload = MemoryPackSerializer.Serialize(info)
                }).ConfigureAwait(false);
                if (resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray))
                {
                    connection.Write(Helper.TrueArray);
                    return;
                }
                connection.Write(Helper.FalseArray);
            }
            catch (Exception)
            {
                connection.Write(Helper.FalseArray);
            }
        }

        private async Task Relay(IConnection source, IConnection target, string secretKey)
        {
            source.Cancel();
            target.Cancel();

            await Task.Delay(100);

            source.TargetStream = target.SourceStream;
            source.TargetSocket = target.SourceSocket;
            source.TargetNetworkStream = target.SourceNetworkStream;
            source.RelayLimit = 0;
            target.TargetStream = source.SourceStream;
            target.TargetSocket = source.SourceSocket;
            target.TargetNetworkStream = source.SourceNetworkStream;
            target.RelayLimit = 0;

            //await Task.Delay(100).ConfigureAwait(false);
            await Task.WhenAll(source.RelayAsync(config.Data.Server.Relay.BufferSize), target.RelayAsync(config.Data.Server.Relay.BufferSize)).ConfigureAwait(false);
        }
        public sealed class TcsWrap
        {
            [JsonIgnore]
            public TaskCompletionSource<IConnection> Tcs { get; set; }
            [JsonIgnore]
            public IConnection Connection { get; set; }
        }
    }


}
