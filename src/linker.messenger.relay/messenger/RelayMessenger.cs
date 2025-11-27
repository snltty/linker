
using linker.libs;
using linker.messenger.relay.server;
using linker.messenger.relay.server.validator;
using linker.messenger.signin;
using linker.tunnel.transport;

namespace linker.messenger.relay.messenger
{
    /// <summary>
    /// 中继客户端
    /// </summary>
    public class RelayClientMessenger : IMessenger
    {
        public RelayClientMessenger()
        {
        }
    }

    /// <summary>
    /// 中继服务端
    /// </summary>
    public class RelayServerMessenger : IMessenger
    {
        private readonly SignInServerCaching signCaching;
        private readonly RelayServerMasterTransfer relayServerTransfer;
        private readonly RelayServerValidatorTransfer relayValidatorTransfer;
        private readonly ISerializer serializer;
        private readonly RelayServerReportResolver relayServerReportResolver;
        private readonly IRelayServerNodeStore relayServerNodeStore;

        public RelayServerMessenger(SignInServerCaching signCaching, ISerializer serializer,
            RelayServerMasterTransfer relayServerTransfer, RelayServerValidatorTransfer relayValidatorTransfer,
            RelayServerReportResolver relayServerReportResolver, IRelayServerNodeStore relayServerNodeStore)
        {
            this.signCaching = signCaching;
            this.relayServerTransfer = relayServerTransfer;
            this.relayValidatorTransfer = relayValidatorTransfer;
            this.serializer = serializer;
            this.relayServerReportResolver = relayServerReportResolver;
            this.relayServerNodeStore = relayServerNodeStore;
        }

        [MessengerId((ushort)RelayMessengerIds.Nodes)]
        public async Task Nodes(IConnection connection)
        {
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) == false)
            {
                connection.Write(serializer.Serialize(new List<RelayNodeStoreInfo> { }));
                return;
            }
            List<RelayNodeStoreInfo> nodes = await GetNodes(cache);
            connection.Write(serializer.Serialize(nodes));
        }

        [MessengerId((ushort)RelayMessengerIds.Ask)]
        public async Task Ask(IConnection connection)
        {
            (string toMachineId, string transactionId, uint flowId) info = serializer.Deserialize<(string toMachineId, string transactionId, uint flowId)>(connection.ReceiveRequestWrap.Payload.Span);

            if (signCaching.TryGet(connection.Id, info.toMachineId, out SignCacheInfo from, out SignCacheInfo to) == false)
            {
                connection.Write(serializer.Serialize(new RelayAskResultInfo()));
                return;
            }

            var nodes = await GetNodes(from).ConfigureAwait(false);
            string error = await relayValidatorTransfer.Validate(from, to, info.transactionId);
            if (string.IsNullOrWhiteSpace(error) == false || relayServerTransfer.AddRelay(from, to, info.flowId) == false)
            {
                connection.Write(serializer.Serialize(new RelayAskResultInfo()));
                return;
            }

            connection.Write(serializer.Serialize(new RelayAskResultInfo { Nodes = nodes, MasterId = relayServerNodeStore.Node.NodeId }));
        }
        private async Task<List<RelayNodeStoreInfo>> GetNodes(SignCacheInfo from)
        {
            return await relayServerTransfer.GetNodes(from.Super, from.UserId, from.MachineId);
        }

        [MessengerId((ushort)RelayMessengerIds.NodeGetCache)]
        public async Task NodeGetCache(IConnection connection)
        {
            relayServerReportResolver.Add(connection.ReceiveRequestWrap.Payload.Length, 0);
            ValueTuple<string, string> key = serializer.Deserialize<ValueTuple<string, string>>(connection.ReceiveRequestWrap.Payload.Span);
            RelayCacheInfo cache = await relayServerTransfer.TryGetRelayCache(key.Item1, key.Item2);
            if (cache != null)
            {
                byte[] sendt = serializer.Serialize(cache);
                relayServerReportResolver.Add(0, sendt.Length);
                connection.Write(sendt);
            }
            else
            {
                connection.Write(Helper.EmptyArray);
            }
        }


        [MessengerId((ushort)RelayMessengerIds.NodeReport)]
        public void NodeReport188(IConnection connection)
        {
            try
            {
                relayServerReportResolver.Add(connection.ReceiveRequestWrap.Payload.Length, 0);
                RelayServerNodeReportInfoOld info = serializer.Deserialize<RelayServerNodeReportInfoOld>(connection.ReceiveRequestWrap.Payload.Span);
            }
            catch (Exception)
            {
            }
            connection.Write(serializer.Serialize(VersionHelper.Version));
        }
    }
}
