
using linker.libs;
using linker.messenger.node;
using linker.messenger.relay.server;
using linker.messenger.relay.server.validator;
using linker.messenger.signin;
using linker.tunnel.transport;
using System.Net;
using static linker.libs.winapis.Wininet;

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
            RelayServerReportResolver relayServerReportResolver, RelayServerMasterTransfer relayServerMasterTransfer,
            RelayServerNodeReportTransfer relayServerNodeReportTransfer)
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
            ValueTuple<string, string, string> kv = serializer.Deserialize<ValueTuple<string, string, string>>(connection.ReceiveRequestWrap.Payload.Span);
            if (await relayServerNodeReportTransfer.SignIn(kv.Item1, kv.Item2, kv.Item3, connection).ConfigureAwait(false))
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
            await relayServerNodeReportTransfer.Report(connection, info).ConfigureAwait(false);
        }


        [MessengerId((ushort)RelayMessengerIds.ShareForward)]
        public async Task ShareForward(IConnection connection)
        {
            string id = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo from) == false || from.Super == false)
            {
                connection.Write(serializer.Serialize("need super key"));
                return;
            }
            connection.Write(serializer.Serialize(await relayServerNodeReportTransfer.GetShareKeyForward(id)));
        }
        [MessengerId((ushort)RelayMessengerIds.Share)]
        public async Task Share(IConnection connection)
        {
            connection.Write(serializer.Serialize(await relayServerNodeReportTransfer.GetShareKey(connection)));
        }

        [MessengerId((ushort)RelayMessengerIds.Import)]
        public async Task Import(IConnection connection)
        {
            string sharekey = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);

            if (signCaching.TryGet(connection.Id, out SignCacheInfo from) == false || from.Super == false)
            {
                connection.Write(serializer.Serialize("need super key"));
                return;
            }

            string result = await relayServerNodeReportTransfer.Import(sharekey).ConfigureAwait(false);
            connection.Write(serializer.Serialize(result));
        }
        [MessengerId((ushort)RelayMessengerIds.Remove)]
        public async Task Remove(IConnection connection)
        {
            string id = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo from) == false || from.Super == false)
            {
                connection.Write(serializer.Serialize("need super key"));
                return;
            }

            bool result = await relayServerNodeReportTransfer.Remove(id).ConfigureAwait(false);
            connection.Write(serializer.Serialize(result ? string.Empty : "remove fail"));
        }
        [MessengerId((ushort)RelayMessengerIds.UpdateForward)]
        public async Task UpdateForward(IConnection connection)
        {
            RelayServerNodeStoreInfo info = serializer.Deserialize<RelayServerNodeStoreInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo from) == false || from.Super == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }

            bool result = await relayServerNodeReportTransfer.UpdateForward(info).ConfigureAwait(false);
            connection.Write(result ? Helper.TrueArray : Helper.FalseArray);
        }
        [MessengerId((ushort)RelayMessengerIds.Update)]
        public async Task Update(IConnection connection)
        {
            RelayServerNodeStoreInfo info = serializer.Deserialize<RelayServerNodeStoreInfo>(connection.ReceiveRequestWrap.Payload.Span);
            await relayServerNodeReportTransfer.Update(connection,info).ConfigureAwait(false);
        }

        [MessengerId((ushort)RelayMessengerIds.UpgradeForward)]
        public async Task UpgradeForward(IConnection connection)
        {
            KeyValuePair<string, string> info = serializer.Deserialize<KeyValuePair<string, string>>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo from) == false || from.Super == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }

            bool result = await relayServerNodeReportTransfer.UpgradeForward(info.Key, info.Value).ConfigureAwait(false);
            connection.Write(result ? Helper.TrueArray : Helper.FalseArray);
        }
        [MessengerId((ushort)RelayMessengerIds.Upgrade)]
        public async Task Upgrade(IConnection connection)
        {
            string version = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            await relayServerNodeReportTransfer.Upgrade(connection, version).ConfigureAwait(false);
        }

        [MessengerId((ushort)RelayMessengerIds.ExitForward)]
        public async Task ExitForward(IConnection connection)
        {
            string nodeid = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo from) == false || from.Super == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }

            bool result = await relayServerNodeReportTransfer.ExitForward(nodeid).ConfigureAwait(false);
            connection.Write(result ? Helper.TrueArray : Helper.FalseArray);
        }
        [MessengerId((ushort)RelayMessengerIds.Exit)]
        public async Task Exit(IConnection connection)
        {
            await relayServerNodeReportTransfer.Exit(connection).ConfigureAwait(false);
        }



        [MessengerId((ushort)RelayMessengerIds.MastersForward)]
        public async Task MastersForward(IConnection connection)
        {
            MastersRequestInfo info = serializer.Deserialize<MastersRequestInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo from) == false || from.Super == false)
            {
                connection.Write(serializer.Serialize(new MastersResponseInfo()));
                return;
            }

            MastersResponseInfo resp = await relayServerNodeReportTransfer.MastersForward(info).ConfigureAwait(false);
            connection.Write(serializer.Serialize(resp));
        }
        [MessengerId((ushort)RelayMessengerIds.Masters)]
        public async Task Masters(IConnection connection)
        {
            MastersRequestInfo info = serializer.Deserialize<MastersRequestInfo>(connection.ReceiveRequestWrap.Payload.Span);
            MastersResponseInfo resp = await relayServerNodeReportTransfer.Masters(connection, info).ConfigureAwait(false);
            connection.Write(serializer.Serialize(resp));
        }


        [MessengerId((ushort)RelayMessengerIds.DenysForward)]
        public async Task DenysForward(IConnection connection)
        {
            MasterDenyStoreRequestInfo info = serializer.Deserialize<MasterDenyStoreRequestInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo from) == false || from.Super == false)
            {
                connection.Write(serializer.Serialize(new MasterDenyStoreResponseInfo()));
                return;
            }

            MasterDenyStoreResponseInfo resp = await relayServerNodeReportTransfer.DenysForward(info).ConfigureAwait(false);
            connection.Write(serializer.Serialize(resp));
        }
        [MessengerId((ushort)RelayMessengerIds.Denys)]
        public async Task Denys(IConnection connection)
        {
            MasterDenyStoreRequestInfo info = serializer.Deserialize<MasterDenyStoreRequestInfo>(connection.ReceiveRequestWrap.Payload.Span);
            MasterDenyStoreResponseInfo resp = await relayServerNodeReportTransfer.Denys(connection, info).ConfigureAwait(false);
            connection.Write(serializer.Serialize(resp));
        }
        [MessengerId((ushort)RelayMessengerIds.DenysAddForward)]
        public async Task DenysAddForward(IConnection connection)
        {
            MasterDenyAddInfo info = serializer.Deserialize<MasterDenyAddInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo from) == false || from.Super == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }

            bool resp = await relayServerNodeReportTransfer.DenysAddForward(info).ConfigureAwait(false);
            connection.Write(resp ? Helper.TrueArray : Helper.FalseArray);
        }
        [MessengerId((ushort)RelayMessengerIds.DenysAdd)]
        public async Task DenysAdd(IConnection connection)
        {
            MasterDenyAddInfo info = serializer.Deserialize<MasterDenyAddInfo>(connection.ReceiveRequestWrap.Payload.Span);
            bool resp = await relayServerNodeReportTransfer.DenysAdd(connection, info).ConfigureAwait(false);
            connection.Write(resp ? Helper.TrueArray : Helper.FalseArray);
        }
        [MessengerId((ushort)RelayMessengerIds.DenysDelForward)]
        public async Task DenysDelForward(IConnection connection)
        {
            MasterDenyDelInfo info = serializer.Deserialize<MasterDenyDelInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo from) == false || from.Super == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }

            bool resp = await relayServerNodeReportTransfer.DenysDelForward(info).ConfigureAwait(false);
            connection.Write(resp ? Helper.TrueArray : Helper.FalseArray);
        }
        [MessengerId((ushort)RelayMessengerIds.DenysDel)]
        public async Task DenysDel(IConnection connection)
        {
            MasterDenyDelInfo info = serializer.Deserialize<MasterDenyDelInfo>(connection.ReceiveRequestWrap.Payload.Span);
            bool resp = await relayServerNodeReportTransfer.DenysDel(connection, info).ConfigureAwait(false);
            connection.Write(resp ? Helper.TrueArray : Helper.FalseArray);
        }


        [MessengerId((ushort)RelayMessengerIds.NodeReport)]
        public async Task NodeReport(IConnection connection)
        {
            try
            {
                relayServerReportResolver.Add(connection.ReceiveRequestWrap.Payload.Length, 0);
                RelayServerNodeReportInfoOld info = serializer.Deserialize<RelayServerNodeReportInfoOld>(connection.ReceiveRequestWrap.Payload.Span);

                if (info.EndPoint.Address.Equals(IPAddress.Any) || info.EndPoint.Address.Equals(IPAddress.Loopback))
                {
                    info.EndPoint.Address = connection.Address.Address;
                }

                await relayServerNodeReportTransfer.Report(info.Id, info.Name, info.EndPoint.ToString()).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
            }
            connection.Write(serializer.Serialize(VersionHelper.Version));
        }

    }
}
