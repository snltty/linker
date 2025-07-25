using linker.libs;
using linker.libs.extends;
using linker.messenger.signin;
using linker.tunnel;
using linker.tunnel.connection;

namespace linker.messenger.pcp
{
    public sealed class PcpTransfer
    {
        private readonly string transactionId = "pcp";

        private readonly IPcpStore pcpStore;
        private readonly TunnelTransfer tunnelTransfer;
        private readonly ISignInClientStore signInClientStore;
        public PcpTransfer(IPcpStore pcpStore, TunnelTransfer tunnelTransfer, ISignInClientStore signInClientStore)
        {
            this.pcpStore = pcpStore;
            this.tunnelTransfer = tunnelTransfer;
            this.signInClientStore = signInClientStore;

            tunnelTransfer.SetConnectedCallback(transactionId, OnConnected);
        }


        /// <summary>
        /// a<->b<->c  在ac通知，b交换数据，不通知
        /// </summary>
        /// <param name="connection"></param>
        private void OnConnected(ITunnelConnection connection)
        {
            TunnelTagInfo tag = connection.TransactionTag.DeJson<TunnelTagInfo>();
            //我是节点
            if (tag.NodeId == signInClientStore.Id)
            {
                return;
            }

            if (OnConnectedCallbacks.TryGetValue(Helper.GlobalString, out List<Action<ITunnelConnection>> callbacks))
            {
                foreach (var item in callbacks)
                {
                    item(connection);
                }
            }
            if (OnConnectedCallbacks.TryGetValue(tag.TransactionId, out callbacks))
            {
                foreach (var item in callbacks)
                {
                    item(connection);
                }
            }
        }
        /// <summary>
        /// 连接
        /// </summary>
        /// <param name="remoteMachineId">目标id</param>
        /// <param name="transactionId">事务ID，属于什么事务的，端口转发还是虚拟网卡</param>
        /// <param name="denyProtocols">不想使用哪些打洞协议</param>
        /// <returns></returns>
        public async Task<ITunnelConnection> ConnectAsync(string remoteMachineId, string transactionId, TunnelProtocolType denyProtocols)
        {
            //TunnelTagInfo tag = new TunnelTagInfo { FromMachineId = signInClientStore.Id, ToMachineId = remoteMachineId, TransactionId = transactionId, NodeId = string.Empty, NodeIds = pcpStore.PcpHistory.History };

            await Task.CompletedTask.ConfigureAwait(false);
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
            if (connection.Type != TunnelType.P2P) return;
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

            /// <summary>
            /// 所有尝试的节点id
            /// </summary>
            public List<string> NodeIds { get; set; }
        }
    }
}
