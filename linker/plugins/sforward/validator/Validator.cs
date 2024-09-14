using linker.config;
using linker.plugins.sforward.config;
using linker.plugins.signin.messenger;

namespace linker.plugins.sforward.validator
{
    /// <summary>
    /// 服务端穿透验证
    /// </summary>
    public sealed class Validator : ISForwardValidator
    {
        private readonly FileConfig config;
        public Validator(FileConfig config)
        {
            this.config = config;
        }

        public async Task<string> Validate(SignCacheInfo signCacheInfo, SForwardAddInfo sForwardAddInfo)
        {
            if (config.Data.Server.SForward.SecretKey != sForwardAddInfo.SecretKey)
            {
                return $"sforward secretKey 【{sForwardAddInfo.SecretKey}】 valid fail";
            }

            if (sForwardAddInfo.RemotePort > 0)
            {
                if (sForwardAddInfo.RemotePort < config.Data.Server.SForward.TunnelPortRange[0] || sForwardAddInfo.RemotePort > config.Data.Server.SForward.TunnelPortRange[1])
                {
                    return $"sforward tunnel port range {string.Join("-", config.Data.Server.SForward.TunnelPortRange)}";
                }
            }
            await Task.CompletedTask;
            return string.Empty;
        }
    }
}
