using linker.messenger.signin;
using RelayInfo = linker.messenger.relay.client.transport.RelayInfo;

namespace linker.messenger.relay.server.validator
{
    public interface IRelayServerValidator
    {
        /// <summary>
        /// 验证
        /// </summary>
        /// <param name="relayInfo">中继信息</param>
        /// <param name="fromMachine">来源客户端</param>
        /// <param name="toMachine">目标客户端，可能为null</param>
        /// <returns></returns>
        public Task<string> Validate(RelayInfo relayInfo, SignCacheInfo fromMachine, SignCacheInfo toMachine);
    }
}
