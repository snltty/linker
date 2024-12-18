using linker.config;
using linker.messenger.signin;
using linker.plugins.sforward.config;

namespace linker.plugins.sforward.validator
{
    /// <summary>
    /// 服务端穿透验证
    /// </summary>
    public sealed class Validator : ISForwardValidator
    {
        private readonly FileConfig config;
        private readonly SForwardServerConfigTransfer sForwardServerConfigTransfer;
        public Validator(FileConfig config, SForwardServerConfigTransfer sForwardServerConfigTransfer)
        {
            this.config = config;
            this.sForwardServerConfigTransfer = sForwardServerConfigTransfer;
        }

        public async Task<string> Validate(SignCacheInfo signCacheInfo, SForwardAddInfo sForwardAddInfo)
        {
            if (sForwardServerConfigTransfer.SecretKey != sForwardAddInfo.SecretKey)
            {
                return $"sforward secretKey 【{sForwardAddInfo.SecretKey}】 valid fail";
            }

            if (sForwardAddInfo.RemotePort > 0)
            {
                if (sForwardAddInfo.RemotePort < sForwardServerConfigTransfer.TunnelPortRange[0] || sForwardAddInfo.RemotePort > sForwardServerConfigTransfer.TunnelPortRange[1])
                {
                    return $"sforward tunnel port range {string.Join("-", sForwardServerConfigTransfer.TunnelPortRange)}";
                }
            }
            await Task.CompletedTask;
            return string.Empty;
        }
    }
}
