using linker.config;
using linker.plugins.relay.transport;
using linker.plugins.signin.messenger;
using linker.libs;
using MemoryPack;
using linker.plugins.messenger;
using linker.plugins.relay.validator;

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
            transport.RelayInfo info = MemoryPackSerializer.Deserialize<transport.RelayInfo>(connection.ReceiveRequestWrap.Payload.Span);
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
        private readonly RelayResolver relayResolver;
        private readonly RelayValidatorTransfer relayValidatorTransfer;


        public RelayServerMessenger(FileConfig config, MessengerSender messengerSender, SignCaching signCaching, RelayResolver relayResolver, RelayValidatorTransfer relayValidatorTransfer)
        {
            this.config = config;
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
            this.relayResolver = relayResolver;
            this.relayValidatorTransfer = relayValidatorTransfer;
        }

        /// <summary>
        /// 测试一下中继通不通
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)RelayMessengerIds.RelayTest)]
        public async Task RelayTest(IConnection connection)
        {
            RelayTestInfo info = MemoryPackSerializer.Deserialize<RelayTestInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }
            string result = await relayValidatorTransfer.Validate(new transport.RelayInfo
            {
                SecretKey = info.SecretKey,
                FromMachineId = info.MachineId,
                FromMachineName = cache.MachineName,
                TransactionId = "test",
                TransportName = "test",
            }, cache, null);
            if (string.IsNullOrWhiteSpace(result) == false)
            {
                connection.Write(ulong.MinValue);
                return;
            }

            connection.Write(Helper.TrueArray);
        }


        /// <summary>
        /// 请求中继
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)RelayMessengerIds.RelayAsk)]
        public async Task RelayAsk(IConnection connection)
        {
            transport.RelayInfo info = MemoryPackSerializer.Deserialize<transport.RelayInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cacheFrom) == false || signCaching.TryGet(info.RemoteMachineId, out SignCacheInfo cacheTo) == false || cacheFrom.GroupId != cacheTo.GroupId)
            {
                connection.Write(ulong.MinValue);
                return;
            }

            info.RemoteMachineId = cacheFrom.MachineId;
            info.FromMachineId = cacheTo.MachineId;
            info.RemoteMachineName = cacheFrom.MachineName;
            info.FromMachineName = cacheTo.MachineName;

            string result = await relayValidatorTransfer.Validate(info, cacheFrom, cacheTo);
            if (string.IsNullOrWhiteSpace(result) == false)
            {
                connection.Write(ulong.MinValue);
                return;
            }

            ulong flowingId = relayResolver.NewRelay(info.FromMachineId, info.RemoteMachineId);
            connection.Write(flowingId);
        }

        /// <summary>
        /// 收到中继请求
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)RelayMessengerIds.RelayForward)]
        public async Task RelayForward(IConnection connection)
        {
            transport.RelayInfo info = MemoryPackSerializer.Deserialize<transport.RelayInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(info.FromMachineId, out SignCacheInfo cacheFrom) == false || signCaching.TryGet(info.RemoteMachineId, out SignCacheInfo cacheTo) == false || cacheFrom.GroupId != cacheTo.GroupId)
            {
                connection.Write(Helper.FalseArray);
                return;
            }

            info.RemoteMachineId = cacheFrom.MachineId;
            info.FromMachineId = cacheTo.MachineId;
            info.RemoteMachineName = cacheFrom.MachineName;
            info.FromMachineName = cacheTo.MachineName;

            string result = await relayValidatorTransfer.Validate(info, cacheFrom, cacheTo);
            if (string.IsNullOrWhiteSpace(result) == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }

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

    }


}
