using linker.messenger.relay.client.transport;

namespace linker.messenger.relay.client
{
    /// <summary>
    /// 中继客户端存储信息
    /// </summary>
    public interface IRelayClientStore
    {
        /// <summary>
        /// 默认中继节点
        /// </summary>
        public string DefaultNodeId { get; }

        /// <summary>
        /// 服务器配置
        /// </summary>
        public RelayServerInfo Server { get; }


        public void SetDefaultNodeId(string defaultNodeId);
        public void SetServer(RelayServerInfo server);
        public void SetServerSecretKey(string secretKey);
    }
}
