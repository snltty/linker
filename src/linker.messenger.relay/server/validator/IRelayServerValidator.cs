using linker.messenger.relay.client.transport;
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
        /// <param name="relayInfo">中继信息</param>
        /// <param name="fromMachine">来源客户端</param>
        /// <param name="toMachine">目标客户端，可能为null</param>
        /// <returns></returns>
        public Task<string> Validate(RelayInfo170 relayInfo, SignCacheInfo fromMachine, SignCacheInfo toMachine);
        /// <summary>
        /// 验证节点
        /// </summary>
        /// <param name="relayInfo"></param>
        /// <param name="fromMachine"></param>
        /// <param name="nodes"></param>
        /// <returns></returns>
        public Task<List<RelayServerNodeReportInfo170>> Validate(string userid, SignCacheInfo fromMachine, List<RelayServerNodeReportInfo170> nodes);
    }
}
