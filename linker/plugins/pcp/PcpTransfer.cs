using linker.libs;
using linker.tunnel;
using linker.tunnel.connection;

namespace linker.plugins.pcp
{
    public sealed class PcpTransfer
    {
        private readonly string transactionId = "pcp";

        private readonly PcpConfigTransfer pcpConfigTransfer;
        private readonly TunnelTransfer tunnelTransfer;
        public PcpTransfer(PcpConfigTransfer pcpConfigTransfer, TunnelTransfer tunnelTransfer)
        {
            this.pcpConfigTransfer = pcpConfigTransfer;
            this.tunnelTransfer = tunnelTransfer;

            tunnelTransfer.SetConnectedCallback(transactionId, OnConnected);
        }
        private void OnConnected(ITunnelConnection connection)
        {
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

        public void AddConnection(ITunnelConnection connection)
        {
            pcpConfigTransfer.AddConnection(connection);
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


    }
}
