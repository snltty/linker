using linker.libs;
using linker.libs.extends;
using linker.messenger.signin;
using linker.tunnel;
using linker.tunnel.connection;
using System.Collections.Concurrent;

namespace linker.messenger.pcp
{
    public sealed class PcpTransfer
    {
        private readonly string transactionId = "pcp";

        private readonly ConcurrentDictionary<string, TaskCompletionSource<bool>> watingDic = new();
        private readonly ConcurrentDictionary<string, ITunnelConnection> swapDic = new();

        private readonly SwapTransfer swapTransfer = new SwapTransfer();

        private readonly IPcpStore pcpStore;
        private readonly TunnelTransfer tunnelTransfer;
        private readonly ISignInClientStore signInClientStore;
        private readonly MessengerSender messengerSender;
        private readonly ISerializer serializer;
        public PcpTransfer(IPcpStore pcpStore, TunnelTransfer tunnelTransfer, ISignInClientStore signInClientStore, MessengerSender messengerSender, ISerializer serializer)
        {
            this.pcpStore = pcpStore;
            this.tunnelTransfer = tunnelTransfer;
            this.signInClientStore = signInClientStore;
            this.messengerSender = messengerSender;
            this.serializer = serializer;
            tunnelTransfer.SetConnectedCallback(transactionId, OnConnected);
        }
        private void OnConnected(ITunnelConnection connection)
        {
            TunnelTagInfo tag = connection.TransactionTag.DeJson<TunnelTagInfo>();
            if (tag.NodeId == signInClientStore.Id)
            {
                string key = $"{tag.FromMachineId}@{tag.ToMachineId}@{tag.TransactionId}";    
                if (swapDic.TryRemove(key,out ITunnelConnection _connection) && _connection.Connected)
                {
                    swapTransfer.Swap(_connection, connection);
                }
                else
                {
                    swapDic.AddOrUpdate(key, connection, (k, v) => connection);
                }

                return;
            }
        }
        public async Task<ITunnelConnection> ConnectAsync(string remoteMachineId, string transactionId, TunnelProtocolType denyProtocols)
        {
            List<string> nodes = pcpStore.PcpHistory.History.Intersect(await GetNodes(remoteMachineId).ConfigureAwait(false)).ToList();

            ITunnelConnection connection = null;
            foreach (var node in nodes)
            {
                try
                {
                    string tag = new TunnelTagInfo { FromMachineId = signInClientStore.Id, ToMachineId = remoteMachineId, TransactionId = transactionId, NodeId = node, DenyProtocols = denyProtocols }.ToJson();
                    connection = await tunnelTransfer.ConnectAsync(node, this.transactionId, denyProtocols, tag).ConfigureAwait(false);
                    if (connection == null)
                    {
                        continue;
                    }
                    await messengerSender.SendOnly(new MessageRequestWrap
                    {
                        MessengerId = (ushort)PcpMessengerIds.BeginForward,
                        Payload = serializer.Serialize(tag),
                    }).ConfigureAwait(false);

                    TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
                    watingDic.AddOrUpdate($"{remoteMachineId}@{transactionId}", tcs, (k, v) => tcs);
                    bool result = await tcs.Task.WaitAsync(TimeSpan.FromMilliseconds(60000)).ConfigureAwait(false);
                    if (result)
                    {
                        ExecConnectedCallbacks(transactionId, connection);
                        return connection;
                    }
                }
                catch (Exception)
                {
                }
                finally
                {
                    watingDic.TryRemove($"{remoteMachineId}@{transactionId}", out _);
                }
            }
            connection?.Dispose();
            return connection;
        }
        public async Task Begin(string tagStr)
        {
            TunnelTagInfo tag = tagStr.DeJson<TunnelTagInfo>();
            ITunnelConnection connection = await tunnelTransfer.ConnectAsync(tag.NodeId, this.transactionId, tag.DenyProtocols, tagStr).ConfigureAwait(false);
            if (connection == null)
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    MessengerId = (ushort)PcpMessengerIds.FailForward,
                    Payload = serializer.Serialize(tagStr),
                }).ConfigureAwait(false);
            }
            else
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    MessengerId = (ushort)PcpMessengerIds.SuccessForward,
                    Payload = serializer.Serialize(tagStr),
                }).ConfigureAwait(false);
                ExecConnectedCallbacks(tag.TransactionId, connection);
            }
        }
        public async Task Fail(string tagStr)
        {
            TunnelTagInfo tag = tagStr.DeJson<TunnelTagInfo>();
            if(watingDic.TryRemove($"{tag.ToMachineId}@{tag.TransactionId}", out TaskCompletionSource<bool> tcs))
            {
                tcs.SetResult(false);
            }
        }
        public async Task Success(string tagStr)
        {
            TunnelTagInfo tag = tagStr.DeJson<TunnelTagInfo>();
            if (watingDic.TryRemove($"{tag.ToMachineId}@{tag.TransactionId}", out TaskCompletionSource<bool> tcs))
            {
                tcs.SetResult(true);
            }
        }
        private async Task<List<string>> GetNodes(string machineId)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                MessengerId = (ushort)PcpMessengerIds.NodesForward,
                Payload = serializer.Serialize(machineId),
                Timeout = 5000,
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK)
            {
                return serializer.Deserialize<List<string>>(resp.Data.Span);
            }
            return [];
        }


        private Dictionary<string, List<Action<ITunnelConnection>>> OnConnectedCallbacks { get; } = new Dictionary<string, List<Action<ITunnelConnection>>>();
        private void ExecConnectedCallbacks(string transactionId, ITunnelConnection connection)
        {
            if (OnConnectedCallbacks.TryGetValue(Helper.GlobalString, out List<Action<ITunnelConnection>> callbacks))
            {
                foreach (var item in callbacks)
                {
                    item(connection);
                }
            }
            if (OnConnectedCallbacks.TryGetValue(transactionId, out callbacks))
            {
                foreach (var item in callbacks)
                {
                    item(connection);
                }
            }
        }
        public void SetConnectedCallback(string transactionId, Action<ITunnelConnection> callback)
        {
            if (OnConnectedCallbacks.TryGetValue(transactionId, out List<Action<ITunnelConnection>> callbacks) == false)
            {
                callbacks = new List<Action<ITunnelConnection>>();
                OnConnectedCallbacks[transactionId] = callbacks;
            }
            callbacks.Add(callback);
        }
        public void RemoveConnectedCallback(string transactionId, Action<ITunnelConnection> callback)
        {
            if (OnConnectedCallbacks.TryGetValue(transactionId, out List<Action<ITunnelConnection>> callbacks))
            {
                callbacks.Remove(callback);
            }
        }

        public void AddConnection(ITunnelConnection connection)
        {
            if (connection.Type != TunnelType.P2P)
            {
                return;
            }
            pcpStore.AddHistory(connection);
        }
        sealed class TunnelTagInfo
        {
            /// <summary>
            /// 谁来的
            /// </summary>
            public string FromMachineId { get; set; }
            /// <summary>
            /// 节点id
            /// </summary>
            public string NodeId { get; set; }
            /// <summary>
            /// 到谁
            /// </summary>
            public string ToMachineId { get; set; }
            /// <summary>
            /// 原本的事务id
            /// </summary>
            public string TransactionId { get; set; }

            public TunnelProtocolType DenyProtocols { get; set; }
        }
    }
}
