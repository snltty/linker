
using linker.messenger.relay.client.transport;
using linker.libs;
using linker.messenger.relay.server;
using linker.messenger.signin;
using linker.messenger.relay.server.validator;

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
        private readonly IMessengerSender messengerSender;
        private readonly SignInServerCaching signCaching;
        private readonly RelayServerMasterTransfer relayServerTransfer;
        private readonly RelayServerValidatorTransfer relayValidatorTransfer;
        private readonly ISerializer serializer;
        private readonly RelayServerNodeTransfer relayServerNodeTransfer;
        private readonly RelayServerReportResolver relayServerReportResolver;
        private readonly ISignInServerStore signInServerStore;

        public RelayServerMessenger(IMessengerSender messengerSender, SignInServerCaching signCaching, ISerializer serializer,
            RelayServerMasterTransfer relayServerTransfer, RelayServerValidatorTransfer relayValidatorTransfer,
            RelayServerNodeTransfer relayServerNodeTransfer, RelayServerReportResolver relayServerReportResolver, ISignInServerStore signInServerStore)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
            this.relayServerTransfer = relayServerTransfer;
            this.relayValidatorTransfer = relayValidatorTransfer;
            this.serializer = serializer;
            this.relayServerNodeTransfer = relayServerNodeTransfer;
            this.relayServerReportResolver = relayServerReportResolver;
            this.signInServerStore = signInServerStore;
        }

        /// <summary>
        /// 测试一下中继通不通
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)RelayMessengerIds.Nodes)]
        public void Nodes(IConnection connection)
        {
            connection.Write(serializer.Serialize(new List<RelayServerNodeReportInfo> { }));
        }
        [MessengerId((ushort)RelayMessengerIds.Nodes170)]
        public async Task Nodes170(IConnection connection)
        {
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) == false)
            {
                connection.Write(serializer.Serialize(new List<RelayServerNodeReportInfo> { }));
                return;
            }
            List<RelayServerNodeReportInfo> nodes = (await GetNodes(cache)).Select(c => (RelayServerNodeReportInfo)c).ToList();
            connection.Write(serializer.Serialize(nodes));
        }
        [MessengerId((ushort)RelayMessengerIds.Nodes188)]
        public async Task Nodes188(IConnection connection)
        {
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) == false)
            {
                connection.Write(serializer.Serialize(new List<RelayServerNodeReportInfo> { }));
                return;
            }
            List<RelayServerNodeReportInfo> nodes = await GetNodes(cache);
            connection.Write(serializer.Serialize(nodes));
        }

        /// <summary>
        /// 请求中继
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)RelayMessengerIds.RelayAsk)]
        public void RelayAsk(IConnection connection)
        {
            connection.Write(serializer.Serialize(new RelayAskResultInfo { }));
        }
        [MessengerId((ushort)RelayMessengerIds.RelayAsk170)]
        public async Task RelayAsk170(IConnection connection)
        {
            RelayInfo info = serializer.Deserialize<RelayInfo>(connection.ReceiveRequestWrap.Payload.Span);

            if (signCaching.TryGet(connection.Id, info.RemoteMachineId, out SignCacheInfo from, out SignCacheInfo to) == false)
            {
                connection.Write(serializer.Serialize(new RelayAskResultInfo { }));
                return;
            }


            var nodes = (await GetNodes(from).ConfigureAwait(false)).Select(c => c).ToList();
            RelayAskResultInfo result = new()
            {
                Nodes = nodes
            };
            if (result.Nodes.Count > 0)
            {
                result.FlowingId = relayServerTransfer.AddRelay(from, to);
            }
            connection.Write(serializer.Serialize(result));
        }
        private async Task<List<RelayServerNodeReportInfo>> GetNodes(SignCacheInfo from)
        {
            return await relayServerTransfer.GetNodes(from.Super, from.UserId, from.MachineId);
        }


        /// <summary>
        /// 收到中继请求
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)RelayMessengerIds.RelayForward)]
        public void RelayForward(IConnection connection)
        {
            connection.Write(Helper.FalseArray);
        }
        [MessengerId((ushort)RelayMessengerIds.RelayForward170)]
        public async Task RelayForward170(IConnection connection)
        {
            RelayInfo info = serializer.Deserialize<RelayInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, info.RemoteMachineId, out SignCacheInfo from, out SignCacheInfo to) == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }

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
                    Payload = serializer.Serialize(info)
                }).ContinueWith(async (result) =>
                {
                    await messengerSender.ReplyOnly(new MessageResponseWrap
                    {
                        RequestId = requiestid,
                        Connection = connection,
                        Payload = result.Result.Data
                    }, (ushort)RelayMessengerIds.RelayForward170).ConfigureAwait(false);
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
                byte[] sendt = serializer.Serialize(cache);
                relayServerReportResolver.Add(connection.ReceiveRequestWrap.Payload.Length, sendt.Length);
                connection.Write(sendt);
            }
            else
            {
                connection.Write(Helper.EmptyArray);
            }
        }
        [MessengerId((ushort)RelayMessengerIds.NodeGetCache186)]
        public async Task NodeGetCache186(IConnection connection)
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
        /// <summary>
        /// 节点报告
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)RelayMessengerIds.NodeReport)]
        public void NodeReport(IConnection connection)
        {
            try
            {
                relayServerReportResolver.Add(connection.ReceiveRequestWrap.Payload.Length, 0);
                RelayServerNodeReportInfo info = serializer.Deserialize<RelayServerNodeReportInfo>(connection.ReceiveRequestWrap.Payload.Span);
                relayServerTransfer.SetNodeReport(connection, info);
            }
            catch (Exception)
            {
            }

            connection.Write(serializer.Serialize(VersionHelper.Version));
        }
        [MessengerId((ushort)RelayMessengerIds.NodeReport188)]
        public void NodeReport188(IConnection connection)
        {
            try
            {
                relayServerReportResolver.Add(connection.ReceiveRequestWrap.Payload.Length, 0);
                RelayServerNodeReportInfo info = serializer.Deserialize<RelayServerNodeReportInfo>(connection.ReceiveRequestWrap.Payload.Span);
                relayServerTransfer.SetNodeReport(connection, info);
            }
            catch (Exception)
            {
            }

            connection.Write(serializer.Serialize(VersionHelper.Version));
        }
        /// <summary>
        /// 更新节点
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)RelayMessengerIds.Edit)]
        public void Edit(IConnection connection)
        {
            RelayServerNodeUpdateInfo info = serializer.Deserialize<RelayServerNodeUpdateInfo>(connection.ReceiveRequestWrap.Payload.Span);
            relayServerNodeTransfer.Edit(info);
        }
        /// <summary>
        /// 更新节点转发
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)RelayMessengerIds.EditForward)]
        public async Task EditForward(IConnection connection)
        {
            RelayServerNodeUpdateWrapInfo info = serializer.Deserialize<RelayServerNodeUpdateWrapInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) && cache.Super)
            {
                await relayServerTransfer.Edit(info.Info).ConfigureAwait(false);
                connection.Write(Helper.TrueArray);
            }
            else
            {
                connection.Write(Helper.FalseArray);
            }
        }
        /// <summary>
        /// 更新节点
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)RelayMessengerIds.Edit188)]
        public void Edit188(IConnection connection)
        {
            RelayServerNodeUpdateInfo info = serializer.Deserialize<RelayServerNodeUpdateInfo>(connection.ReceiveRequestWrap.Payload.Span);
            relayServerNodeTransfer.Edit(info);
        }
        /// <summary>
        /// 更新节点转发
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)RelayMessengerIds.EditForward188)]
        public async Task EditForward188(IConnection connection)
        {
            RelayServerNodeUpdateWrapInfo info = serializer.Deserialize<RelayServerNodeUpdateWrapInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) && cache.Super)
            {
                await relayServerTransfer.Edit(info.Info).ConfigureAwait(false);
                connection.Write(Helper.TrueArray);
            }
            else
            {
                connection.Write(Helper.FalseArray);
            }
        }

        /// <summary>
        /// 重启
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)RelayMessengerIds.Exit)]
        public void Exit(IConnection connection)
        {
            relayServerNodeTransfer.Exit();
        }
        /// <summary>
        /// 重启节点转发
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)RelayMessengerIds.ExitForward)]
        public async Task ExitForward(IConnection connection)
        {
            string id = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) && cache.Super)
            {
                await relayServerTransfer.Exit(id).ConfigureAwait(false);
                connection.Write(Helper.TrueArray);
            }
            else
            {
                connection.Write(Helper.FalseArray);
            }
        }


        /// <summary>
        /// 更新
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)RelayMessengerIds.Update)]
        public void Update(IConnection connection)
        {
            relayServerNodeTransfer.Update(serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span));
        }
        /// <summary>
        /// 更新转发
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)RelayMessengerIds.UpdateForward)]
        public async Task UpdateForward(IConnection connection)
        {
            KeyValuePair<string, string> info = serializer.Deserialize<KeyValuePair<string, string>>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) && cache.Super)
            {
                await relayServerTransfer.Update(info.Key, info.Value).ConfigureAwait(false);
                connection.Write(Helper.TrueArray);
            }
            else
            {
                connection.Write(Helper.FalseArray);
            }
        }

       

        [MessengerId((ushort)RelayMessengerIds.Hosts)]
        public void Hosts(IConnection connection)
        {
            connection.Write(serializer.Serialize(signInServerStore.Hosts));
        }

    }
}
