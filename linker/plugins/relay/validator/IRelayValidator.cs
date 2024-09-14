using linker.plugins.signin.messenger;

namespace linker.plugins.relay.validator
{
    public interface IRelayValidator
    {
        /// <summary>
        /// 验证
        /// </summary>
        /// <param name="fromMachine">来源客户端</param>
        /// <param name="toMachine">目标客户端，可能为null</param>
        /// <returns></returns>
        public Task<string> Validate(SignCacheInfo fromMachine, SignCacheInfo toMachine);
    }
}
