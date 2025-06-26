
using linker.messenger.relay.client.transport;
using linker.libs;
using linker.messenger.relay.client;
using linker.messenger.relay.server;
using linker.messenger.signin;
using linker.messenger.relay.server.validator;
using linker.messenger.cdkey;

namespace linker.messenger.relay.messenger
{
    /// <summary>
    /// 中继客户端
    /// </summary>
    public class RelayClientMessenger : IMessenger
    {
        private readonly RelayClientTransfer relayTransfer;
        private readonly ISerializer serializer;
        public RelayClientMessenger(RelayClientTransfer relayTransfer, ISerializer serializer)
        {
            this.relayTransfer = relayTransfer;
            this.serializer = serializer;
        }

        /// <summary>
        /// 收到中继请求
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)RelayMessengerIds.Relay)]
        public async Task Relay(IConnection connection)
        {
            client.transport.RelayInfo170 info = serializer.Deserialize<client.transport.RelayInfo170>(connection.ReceiveRequestWrap.Payload.Span);
            bool res = await relayTransfer.OnBeginAsync(info).ConfigureAwait(false);
            connection.Write(res ? Helper.TrueArray : Helper.FalseArray);
        }
    }

    /// <summary>
    /// 中继服务端
    /// </summary>
    public class RelayServerMessenger : IMessenger
    {
        private readonly IMessengerSender messengerSender;
        private readonly SignInServerCaching signCaching;
        private readonly RelayServerMasterTransfer relayServerTransfer;
        private readonly RelayServerValidatorTransfer relayValidatorTransfer;
        private readonly ISerializer serializer;
        private readonly IRelayServerStore relayServerStore;
        private readonly RelayServerNodeTransfer relayServerNodeTransfer;
        private readonly IRelayServerUser2NodeStore relayServerUser2NodeStore;

        public RelayServerMessenger(IMessengerSender messengerSender, SignInServerCaching signCaching, ISerializer serializer, RelayServerMasterTransfer relayServerTransfer, RelayServerValidatorTransfer relayValidatorTransfer, IRelayServerStore relayServerStore, RelayServerNodeTransfer relayServerNodeTransfer, IRelayServerUser2NodeStore relayServerUser2NodeStore)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
            this.relayServerTransfer = relayServerTransfer;
            this.relayValidatorTransfer = relayValidatorTransfer;
            this.serializer = serializer;
            this.relayServerStore = relayServerStore;
            this.relayServerNodeTransfer = relayServerNodeTransfer;
            this.relayServerUser2NodeStore = relayServerUser2NodeStore;
        }

        /// <summary>
        /// 测试一下中继通不通
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)RelayMessengerIds.RelayTest)]
        public async Task RelayTest(IConnection connection)
        {
            RelayTestInfo info = serializer.Deserialize<RelayTestInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) == false)
            {
                connection.Write(serializer.Serialize(new List<RelayServerNodeReportInfo> { }));
                return;
            }
            (List<RelayServerNodeReportInfo170> nodes, bool validated) = await GetNodes(new client.transport.RelayInfo170
            {
                SecretKey = info.SecretKey,
                FromMachineId = info.MachineId,
                FromMachineName = cache.MachineName,
                TransactionId = "test",
                TransportName = "test",
            }, cache, null);
            connection.Write(serializer.Serialize(nodes.Select(c => (RelayServerNodeReportInfo)c).ToList()));
        }
        [MessengerId((ushort)RelayMessengerIds.RelayTest170)]
        public async Task RelayTest170(IConnection connection)
        {
            RelayTestInfo170 info = serializer.Deserialize<RelayTestInfo170>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) == false)
            {
                connection.Write(serializer.Serialize(new List<RelayServerNodeReportInfo170> { }));
                return;
            }
            (List<RelayServerNodeReportInfo170> nodes, bool validated) = await GetNodes(new client.transport.RelayInfo170
            {
                SecretKey = info.SecretKey,
                FromMachineId = info.MachineId,
                FromMachineName = cache.MachineName,
                UserId = info.UserId,
                TransactionId = "test",
                TransportName = "test",
            }, cache, null);
            connection.Write(serializer.Serialize(nodes));
        }

        /// <summary>
        /// 请求中继
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)RelayMessengerIds.RelayAsk)]
        public async Task RelayAsk(IConnection connection)
        {
            RelayInfo info = serializer.Deserialize<RelayInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, info.RemoteMachineId, out SignCacheInfo from, out SignCacheInfo to) == false)
            {
                connection.Write(serializer.Serialize(new RelayAskResultInfo { }));
                return;
            }

            info.RemoteMachineId = to.MachineId;
            info.FromMachineId = from.MachineId;
            info.RemoteMachineName = to.MachineName;
            info.FromMachineName = from.MachineName;

            RelayAskResultInfo result = new RelayAskResultInfo();
            (List<RelayServerNodeReportInfo170> nodes, bool validated) = await GetNodes(info, from, to);
            result.Nodes = nodes.Select(c => (RelayServerNodeReportInfo)c).ToList();
            if (result.Nodes.Count > 0)
            {
                result.FlowingId = relayServerTransfer.AddRelay(from.MachineId, from.MachineName, to.MachineId, to.MachineName, from.GroupId, string.Empty, validated, false);
            }
            connection.Write(serializer.Serialize(result));
        }
        [MessengerId((ushort)RelayMessengerIds.RelayAsk170)]
        public async Task RelayAsk170(IConnection connection)
        {
            RelayInfo170 info = serializer.Deserialize<RelayInfo170>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, info.RemoteMachineId, out SignCacheInfo from, out SignCacheInfo to) == false)
            {
                connection.Write(serializer.Serialize(new RelayAskResultInfo170 { }));
                return;
            }

            info.RemoteMachineId = to.MachineId;
            info.FromMachineId = from.MachineId;
            info.RemoteMachineName = to.MachineName;
            info.FromMachineName = from.MachineName;

            RelayAskResultInfo170 result = new RelayAskResultInfo170();
            (result.Nodes, bool validated) = await GetNodes(info, from, to).ConfigureAwait(false);
            if (result.Nodes.Count > 0)
            {
                result.FlowingId = relayServerTransfer.AddRelay(from.MachineId, from.MachineName, to.MachineId, to.MachineName, from.GroupId, info.UserId, validated, info.UseCdkey);
            }

            connection.Write(serializer.Serialize(result));
        }

        private async Task<(List<RelayServerNodeReportInfo170>, bool)> GetNodes(RelayInfo relayInfo, SignCacheInfo from, SignCacheInfo to)
        {
            string error = await relayValidatorTransfer.Validate(relayInfo, from, to).ConfigureAwait(false);
            bool validated = string.IsNullOrWhiteSpace(error);
            return (await relayServerTransfer.GetNodes(validated, string.Empty), validated);
        }
        private async Task<(List<RelayServerNodeReportInfo170>, bool)> GetNodes(RelayInfo170 relayInfo, SignCacheInfo from, SignCacheInfo to)
        {
            string error = await relayValidatorTransfer.Validate(relayInfo, from, to).ConfigureAwait(false);
            bool validated = string.IsNullOrWhiteSpace(error);
            return (await relayServerTransfer.GetNodes(validated, relayInfo.UserId), validated);
        }
       

        /// <summary>
        /// 收到中继请求
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)RelayMessengerIds.RelayForward)]
        public async Task RelayForward(IConnection connection)
        {
            RelayInfo info = serializer.Deserialize<RelayInfo>(connection.ReceiveRequestWrap.Payload.Span);
            await RelayForward(connection, info, (ushort)RelayMessengerIds.RelayForward, () =>
            {
                return serializer.Serialize(info);
            }).ConfigureAwait(false);
        }
        [MessengerId((ushort)RelayMessengerIds.RelayForward170)]
        public async Task RelayForward170(IConnection connection)
        {
            RelayInfo170 info = serializer.Deserialize<RelayInfo170>(connection.ReceiveRequestWrap.Payload.Span);
            await RelayForward(connection, info, (ushort)RelayMessengerIds.RelayForward170, () =>
            {
                return serializer.Serialize(info);
            }).ConfigureAwait(false);
        }
        public async Task RelayForward(IConnection connection, RelayInfo info, ushort id, Func<byte[]> data)
        {
            if (signCaching.TryGet(connection.Id, info.RemoteMachineId, out SignCacheInfo from, out SignCacheInfo to) == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }

            //需要验证
            if (relayServerTransfer.NodeValidate(info.NodeId))
            {
                info.RemoteMachineId = to.MachineId;
                info.FromMachineId = from.MachineId;
                info.RemoteMachineName = to.MachineName;
                info.FromMachineName = from.MachineName;
                string result = await relayValidatorTransfer.Validate(info, from, to).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(result) == false)
                {
                    connection.Write(Helper.FalseArray);
                    return;
                }
            }

            info.RemoteMachineId = from.MachineId;
            info.FromMachineId = to.MachineId;
            info.RemoteMachineName = from.MachineName;
            info.FromMachineName = to.MachineName;
            try
            {
                uint requiestid = connection.ReceiveRequestWrap.RequestId;
                _ = messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)RelayMessengerIds.Relay,
                    Payload = data()
                }).ContinueWith(async (result) =>
                {
                    await messengerSender.ReplyOnly(new MessageResponseWrap
                    {
                        RequestId = requiestid,
                        Connection = connection,
                        Payload = result.Result.Data
                    }, id).ConfigureAwait(false);
                }).ConfigureAwait(false);
            }
            catch (Exception)
            {
                connection.Write(Helper.FalseArray);
            }
        }

        /// <summary>
        /// 获取缓存
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)RelayMessengerIds.NodeGetCache)]
        public async Task NodeGetCache(IConnection connection)
        {
            string key = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            RelayCacheInfo cache = await relayServerTransfer.TryGetRelayCache(key, string.Empty);
            if (cache != null)
            {
                connection.Write(serializer.Serialize(cache));
            }
            else
            {
                connection.Write(Helper.EmptyArray);
            }
        }
        [MessengerId((ushort)RelayMessengerIds.NodeGetCache186)]
        public async Task NodeGetCache186(IConnection connection)
        {
            ValueTuple<string, string> key = serializer.Deserialize<ValueTuple<string, string>>(connection.ReceiveRequestWrap.Payload.Span);
            RelayCacheInfo cache = await relayServerTransfer.TryGetRelayCache(key.Item1, key.Item2);
            if (cache != null)
            {
                connection.Write(serializer.Serialize(cache));
            }
            else
            {
                connection.Write(Helper.EmptyArray);
            }
        }
        /// <summary>
        /// 节点报告
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)RelayMessengerIds.NodeReport)]
        public void NodeReport(IConnection connection)
        {
            RelayServerNodeReportInfo170 info = serializer.Deserialize<RelayServerNodeReportInfo170>(connection.ReceiveRequestWrap.Payload.Span);
            relayServerTransfer.SetNodeReport(connection, info);
        }
        /// <summary>
        /// 更新节点
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)RelayMessengerIds.UpdateNode)]
        public void UpdateNode(IConnection connection)
        {
            RelayServerNodeUpdateInfo info = serializer.Deserialize<RelayServerNodeUpdateInfo>(connection.ReceiveRequestWrap.Payload.Span);
            relayServerNodeTransfer.UpdateNode(info);
        }
        /// <summary>
        /// 更新节点转发
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)RelayMessengerIds.UpdateNodeForward)]
        public async Task UpdateNodeForward(IConnection connection)
        {
            RelayServerNodeUpdateWrapInfo info = serializer.Deserialize<RelayServerNodeUpdateWrapInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (relayServerStore.ValidateSecretKey(info.SecretKey))
            {
                await relayServerTransfer.UpdateNodeReport(info.Info).ConfigureAwait(false);
                connection.Write(Helper.TrueArray);
            }
            else
            {
                connection.Write(Helper.FalseArray);
            }
        }

        /// <summary>
        /// 消耗流量报告
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)RelayMessengerIds.TrafficReport)]
        public void TrafficReport(IConnection connection)
        {
            RelayTrafficUpdateInfo info = serializer.Deserialize<RelayTrafficUpdateInfo>(connection.ReceiveRequestWrap.Payload.Span);

            relayServerTransfer.AddTraffic(info);
        }
        /// <summary>
        /// 下发剩余流量
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)RelayMessengerIds.SendLastBytes)]
        public void SendLastBytes(IConnection connection)
        {
            Dictionary<int, long> info = serializer.Deserialize<Dictionary<int, long>>(connection.ReceiveRequestWrap.Payload.Span);
            relayServerNodeTransfer.UpdateLastBytes(info);
        }

        /// <summary>
        /// 检查密钥
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)RelayMessengerIds.CheckKey)]
        public void CheckKey(IConnection connection)
        {
            string key = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            connection.Write(relayServerStore.ValidateSecretKey(key) ? Helper.TrueArray : Helper.FalseArray);
        }



        /// <summary>
        /// 添加CDKEY
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)RelayMessengerIds.AddUser2Node)]
        public async Task AddUser2Node(IConnection connection)
        {
            RelayServerUser2NodeAddInfo info = serializer.Deserialize<RelayServerUser2NodeAddInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }
            if (relayServerStore.ValidateSecretKey(info.SecretKey) == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }

            await relayServerUser2NodeStore.Add(info.Data).ConfigureAwait(false);
            connection.Write(Helper.TrueArray);
        }

        /// <summary>
        /// 删除Cdkey
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)RelayMessengerIds.DelUser2Node)]
        public async Task DelUser2Node(IConnection connection)
        {
            RelayServerUser2NodeDelInfo info = serializer.Deserialize<RelayServerUser2NodeDelInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }
            if (relayServerStore.ValidateSecretKey(info.SecretKey) == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }
            await relayServerUser2NodeStore.Del(info.Id).ConfigureAwait(false);
            connection.Write(Helper.TrueArray);
        }

        /// <summary>
        /// 查询CDKEY
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)RelayMessengerIds.PageUser2Node)]
        public async Task PageUser2Node(IConnection connection)
        {
            RelayServerUser2NodePageRequestInfo info = serializer.Deserialize<RelayServerUser2NodePageRequestInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) == false)
            {
                connection.Write(serializer.Serialize(new RelayServerUser2NodePageResultInfo { }));
                return;
            }
            if (relayServerStore.ValidateSecretKey(info.SecretKey) == false && string.IsNullOrWhiteSpace(info.UserId))
            {
                connection.Write(serializer.Serialize(new RelayServerUser2NodePageResultInfo { }));
                return;
            }

            var page = await relayServerUser2NodeStore.Page(info).ConfigureAwait(false);

            connection.Write(serializer.Serialize(page));
        }

    }
}
