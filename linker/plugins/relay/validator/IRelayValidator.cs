using linker.config;
using linker.plugins.action;
using linker.plugins.messenger;
using linker.plugins.relay.transport;
using linker.plugins.signin.messenger;
using RelayInfo = linker.plugins.relay.transport.RelayInfo;

namespace linker.plugins.relay.validator
{
    public interface IRelayValidator
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

    public sealed class RelayValidatorSecretKey : IRelayValidator
    {
        private readonly FileConfig fileConfig;

        public RelayValidatorSecretKey(FileConfig fileConfig)
        {
            this.fileConfig = fileConfig;
        }

        public async Task<string> Validate(linker.plugins.relay.transport.RelayInfo relayInfo, SignCacheInfo fromMachine, SignCacheInfo toMachine)
        {
            if (relayInfo.SecretKey != fileConfig.Data.Server.Relay.SecretKey)
            {
                return $"SecretKey validate fail";
            }

            await Task.CompletedTask;
            return string.Empty;
        }
    }
}
