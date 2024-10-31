using linker.config;
using linker.plugins.relay.client.transport;
using linker.plugins.signin.messenger;
using linker.libs;
using MemoryPack;
using linker.plugins.messenger;
using System.Net;
using linker.plugins.relay.client;
using linker.plugins.relay.server.validator;
using linker.plugins.relay.server;

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
            client.transport.RelayInfo info = MemoryPackSerializer.Deserialize<client.transport.RelayInfo>(connection.ReceiveRequestWrap.Payload.Span);
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
        private readonly IMessengerSender messengerSender;
        private readonly SignCaching signCaching;
        private readonly RelayServerTransfer relayServerTransfer;
        private readonly RelayValidatorTransfer relayValidatorTransfer;


        public RelayServerMessenger(FileConfig config, IMessengerSender messengerSender, SignCaching signCaching, RelayServerTransfer relayServerTransfer, RelayValidatorTransfer relayValidatorTransfer)
        {
            this.config = config;
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
            this.relayServerTransfer = relayServerTransfer;
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
            string result = await relayValidatorTransfer.Validate(new client.transport.RelayInfo
            {
                SecretKey = info.SecretKey,
                FromMachineId = info.MachineId,
                FromMachineName = cache.MachineName,
                TransactionId = "test",
                TransportName = "test",
            }, cache, null);
            if (string.IsNullOrWhiteSpace(result) == false)
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
        public async Task RelayAsk(IConnection connection)
        {
            client.transport.RelayInfo info = MemoryPackSerializer.Deserialize<client.transport.RelayInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cacheFrom) == false || signCaching.TryGet(info.RemoteMachineId, out SignCacheInfo cacheTo) == false || cacheFrom.GroupId != cacheTo.GroupId)
            {
                connection.Write(MemoryPackSerializer.Serialize(new RelayAskResultInfo { }));
                return;
            }

            info.RemoteMachineId = cacheTo.MachineId;
            info.FromMachineId = cacheFrom.MachineId;
            info.RemoteMachineName = cacheTo.MachineName;
            info.FromMachineName = cacheFrom.MachineName;

            RelayAskResultInfo result = new RelayAskResultInfo();
            string error = await relayValidatorTransfer.Validate(info, cacheFrom, cacheTo);
            result.Nodes = relayServerTransfer.GetNodes(string.IsNullOrWhiteSpace(error));

            if (result.Nodes.Count > 0)
            {
                result.FlowingId = await relayServerTransfer.AddRelay(cacheFrom.MachineId, cacheFrom.MachineName, cacheTo.MachineId, cacheTo.MachineName);
            }

            connection.Write(MemoryPackSerializer.Serialize(result));
        }

        /// <summary>
        /// 收到中继请求
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)RelayMessengerIds.RelayForward)]
        public async Task RelayForward(IConnection connection)
        {
            client.transport.RelayInfo info = MemoryPackSerializer.Deserialize<client.transport.RelayInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(info.FromMachineId, out SignCacheInfo cacheFrom) == false || signCaching.TryGet(info.RemoteMachineId, out SignCacheInfo cacheTo) == false || cacheFrom.GroupId != cacheTo.GroupId)
            {
                connection.Write(Helper.FalseArray);
                return;
            }    

            //需要验证
            if (relayServerTransfer.NodeValidate(info.NodeId))
            {
                info.RemoteMachineId = cacheTo.MachineId;
                info.FromMachineId = cacheFrom.MachineId;
                info.RemoteMachineName = cacheTo.MachineName;
                info.FromMachineName = cacheFrom.MachineName;
                string result = await relayValidatorTransfer.Validate(info, cacheFrom, cacheTo);
                if (string.IsNullOrWhiteSpace(result) == false)
                {
                    connection.Write(Helper.FalseArray);
                    return;
                }
            }
            //本服务器，让对方按信标服务器地址连
            if (string.IsNullOrWhiteSpace(info.NodeId))
            {
                info.Server = null;
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

    }

    [MemoryPackable]
    public sealed partial class RelayAskResultInfo
    {
        public ulong FlowingId { get; set; }

        [MemoryPackAllowSerialize]
        public List<RelayNodeReportInfo> Nodes { get; set; } = new List<RelayNodeReportInfo>();
    }

}
