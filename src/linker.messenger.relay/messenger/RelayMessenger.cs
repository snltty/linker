
using linker.libs;
using linker.messenger.relay.server;
using linker.messenger.relay.server.validator;
using linker.messenger.signin;
using linker.tunnel.transport;
using System.Net;

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
        private readonly RelayServerValidatorTransfer relayValidatorTransfer;
        private readonly ISerializer serializer;
        private readonly RelayServerReportResolver relayServerReportResolver;
        private readonly RelayServerMasterTransfer relayServerMasterTransfer;
        private readonly RelayServerNodeReportTransfer relayServerNodeReportTransfer;

        public RelayServerMessenger(SignInServerCaching signCaching, ISerializer serializer, RelayServerValidatorTransfer relayValidatorTransfer,
            RelayServerReportResolver relayServerReportResolver, RelayServerMasterTransfer relayServerMasterTransfer, RelayServerNodeReportTransfer relayServerNodeReportTransfer)
        {
            this.signCaching = signCaching;
            this.relayValidatorTransfer = relayValidatorTransfer;
            this.serializer = serializer;
            this.relayServerReportResolver = relayServerReportResolver;
            this.relayServerMasterTransfer = relayServerMasterTransfer;
            this.relayServerNodeReportTransfer = relayServerNodeReportTransfer;
        }


        [MessengerId((ushort)RelayMessengerIds.Nodes)]
        public async Task Nodes(IConnection connection)
        {
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) == false)
            {
                connection.Write(serializer.Serialize(new List<RelayServerNodeStoreInfo> { }));
                return;
            }
            List<RelayServerNodeStoreInfo> nodes = await GetNodes(cache);
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
            if (string.IsNullOrWhiteSpace(error) == false || relayServerMasterTransfer.AddRelay(from, to, info.flowId) == false)
            {
                connection.Write(serializer.Serialize(new RelayAskResultInfo()));
                return;
            }

            connection.Write(serializer.Serialize(new RelayAskResultInfo { Nodes = nodes, MasterId = relayServerNodeReportTransfer.Config.NodeId }));
        }

        private async Task<List<RelayServerNodeStoreInfo>> GetNodes(SignCacheInfo from)
        {
            return await relayServerNodeReportTransfer.GetNodes(from.Super, from.UserId, from.MachineId);
        }



        [MessengerId((ushort)RelayMessengerIds.GetCache)]
        public async Task GetCache(IConnection connection)
        {
            relayServerReportResolver.Add(connection.ReceiveRequestWrap.Payload.Length, 0);
            ValueTuple<string, string> key = serializer.Deserialize<ValueTuple<string, string>>(connection.ReceiveRequestWrap.Payload.Span);
            RelayCacheInfo cache = await relayServerMasterTransfer.TryGetRelayCache(key.Item1, key.Item2);
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

        [MessengerId((ushort)RelayMessengerIds.SignIn)]
        public async Task SignIn(IConnection connection)
        {
            KeyValuePair<string, string> kv = serializer.Deserialize<KeyValuePair<string, string>>(connection.ReceiveRequestWrap.Payload.Span);
            if (await relayServerNodeReportTransfer.SignIn(kv.Key, kv.Value, connection).ConfigureAwait(false))
            {
                connection.Write(Helper.TrueArray);
            }
            else
            {
                connection.Write(Helper.FalseArray);
            }
        }
        [MessengerId((ushort)RelayMessengerIds.Report)]
        public async Task Report(IConnection connection)
        {
            RelayServerNodeReportInfo info = serializer.Deserialize<RelayServerNodeReportInfo>(connection.ReceiveRequestWrap.Payload.Span);
            await relayServerNodeReportTransfer.Report(info).ConfigureAwait(false);
        }


        [MessengerId((ushort)RelayMessengerIds.NodeReport)]
        public async Task NodeReport(IConnection connection)
        {
            try
            {
                relayServerReportResolver.Add(connection.ReceiveRequestWrap.Payload.Length, 0);
                RelayServerNodeReportInfoOld info = serializer.Deserialize<RelayServerNodeReportInfoOld>(connection.ReceiveRequestWrap.Payload.Span);

                if (info.EndPoint.Address.Equals(IPAddress.Any))
                {
                    info.EndPoint.Address = connection.Address.Address;
                }

                await relayServerNodeReportTransfer.Report(info.Id, info.Name, info.EndPoint.ToString()).ConfigureAwait(false);
            }
            catch (Exception)
            {
            }
            connection.Write(serializer.Serialize(VersionHelper.Version));
        }
    }
}
