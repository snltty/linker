using linker.messenger.signin;

namespace linker.messenger.relay.server.validator
{
    /// <summary>
    /// 中继验证
    /// </summary>
    public interface IRelayServerValidator
    {
         public string Name { get; }
        /// <summary>
        /// 验证
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="transactionId"></param>
        /// <returns></returns>
        public Task<string> Validate(SignCacheInfo from, SignCacheInfo to,string transactionId);
        /// <summary>
        /// 验证节点
        /// </summary>
        /// <param name="relayInfo"></param>
        /// <param name="fromMachine"></param>
        /// <param name="nodes"></param>
        /// <returns></returns>
        public Task<List<RelayServerNodeStoreInfo>> Validate(string userid, SignCacheInfo from, List<RelayServerNodeStoreInfo> nodes);
    }
}
