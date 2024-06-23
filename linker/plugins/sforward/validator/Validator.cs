using linker.config;
using linker.plugins.sforward.config;
using linker.server;

namespace linker.plugins.sforward.validator
{
    public sealed class Validator : IValidator
    {
        private readonly Config config;
        public Validator(Config config)
        {
            this.config = config;
        }

        public bool Valid(IConnection connection, SForwardAddInfo sForwardAddInfo, out string error)
        {
            error = string.Empty;

            if (config.Data.Server.SForward.SecretKey != sForwardAddInfo.SecretKey)
            {
                error = $"sforward secretKey 【{sForwardAddInfo.SecretKey}】 valid fail";
                return false;
            }

            if (sForwardAddInfo.RemotePort > 0)
            {
                if (sForwardAddInfo.RemotePort < config.Data.Server.SForward.TunnelPortRange[0] || sForwardAddInfo.RemotePort > config.Data.Server.SForward.TunnelPortRange[1])
                {
                    error = $"sforward tunnel port range {string.Join("-", config.Data.Server.SForward.TunnelPortRange)}";
                    return false;
                }
            }

            return true;
        }
    }
}
