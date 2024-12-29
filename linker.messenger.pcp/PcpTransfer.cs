using linker.libs;
using linker.libs.extends;
using linker.tunnel;
using linker.tunnel.connection;

namespace linker.messenger.pcp
{
    public sealed class PcpTransfer
    {
        private readonly string transactionId = "pcp";

        private readonly IPcpStore pcpStore;
        private readonly TunnelTransfer tunnelTransfer;
        public PcpTransfer(IPcpStore pcpStore, TunnelTransfer tunnelTransfer)
        {
            this.pcpStore = pcpStore;
            this.tunnelTransfer = tunnelTransfer;

            tunnelTransfer.SetConnectedCallback(transactionId, OnConnected);
        }

        private void OnConnected(ITunnelConnection connection)
        {
            TunnelTagInfo tag = connection.TransactionTag.DeJson<TunnelTagInfo>();

            //connection.TransactionId = tag.OriginTransactionId;
            // connection.Type = TunnelType.Node;
            if (OnConnectedCallbacks.TryGetValue(Helper.GlobalString, out List<Action<ITunnelConnection>> callbacks))
            {
                foreach (var item in callbacks)
                {
                    item(connection);
                }
            }
            if (OnConnectedCallbacks.TryGetValue(connection.TransactionId, out callbacks))
            {
                foreach (var item in callbacks)
                {
                    item(connection);
                }
            }
        }
        public async Task<ITunnelConnection> ConnectAsync(string remoteMachineId, string transactionId, TunnelProtocolType denyProtocols)
        {
            await Task.CompletedTask;
            return null;
        }

        private Dictionary<string, List<Action<ITunnelConnection>>> OnConnectedCallbacks { get; } = new Dictionary<string, List<Action<ITunnelConnection>>>();
        /// <summary>
        /// 设置成功回调
        /// </summary>
        /// <param name="transactionId">事务</param>
        /// <param name="callback"></param>
        public void SetConnectedCallback(string transactionId, Action<ITunnelConnection> callback)
        {
            if (OnConnectedCallbacks.TryGetValue(transactionId, out List<Action<ITunnelConnection>> callbacks) == false)
            {
                callbacks = new List<Action<ITunnelConnection>>();
                OnConnectedCallbacks[transactionId] = callbacks;
            }
            callbacks.Add(callback);
        }
        /// <summary>
        /// 移除成功回调
        /// </summary>
        /// <param name="transactionId">事务</param>
        /// <param name="callback"></param>
        public void RemoveConnectedCallback(string transactionId, Action<ITunnelConnection> callback)
        {
            if (OnConnectedCallbacks.TryGetValue(transactionId, out List<Action<ITunnelConnection>> callbacks))
            {
                callbacks.Remove(callback);
            }
        }


        public void AddConnection(ITunnelConnection connection)
        {
            pcpStore.AddHistory(connection);
        }

        sealed class TunnelTagInfo
        {
            /// <summary>
            /// 节点id
            /// </summary>
            public string NodeId { get; set; }
            /// <summary>
            /// 原本的事务id
            /// </summary>
            public string TransactionId { get; set; }
            /// <summary>
            /// 是哪边的，l 和 r
            /// </summary>
            public char Side { get; set; } = 'l';
        }
    }
}
