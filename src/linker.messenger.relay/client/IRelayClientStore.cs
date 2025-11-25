using linker.tunnel.connection;

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
        /// 默认中继协议
        /// </summary>
        public TunnelProtocolType DefaultProtocol { get; }

        /// <summary>
        /// 设置默认节点id
        /// </summary>
        /// <param name="nodeId"></param>
        public void SetDefaultNodeId(string nodeId);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="protocol"></param>
        public void SetDefaultProtocol(TunnelProtocolType protocol);

        /// <summary>
        /// 提交
        /// </summary>
        /// <returns></returns>
        public bool Confirm();
    }
}
