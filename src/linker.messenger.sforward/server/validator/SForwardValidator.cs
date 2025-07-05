using linker.messenger.signin;

namespace linker.messenger.sforward.server.validator
{
    /// <summary>
    /// 服务端穿透验证
    /// </summary>
    public sealed class SForwardValidator : ISForwardValidator
    {
        public string Name => "default";

        private readonly ISForwardServerStore sForwardServerStore;
        public SForwardValidator(ISForwardServerStore sForwardServerStore)
        {
            this.sForwardServerStore = sForwardServerStore;
        }

        public async Task<string> Validate(SignCacheInfo signCacheInfo, SForwardAddInfo sForwardAddInfo)
        {
            if (signCacheInfo.Super)
            {
                return $"need super key and password";
            }

            if (sForwardAddInfo.RemotePort > 0)
            {
                if (sForwardAddInfo.RemotePort < sForwardServerStore.TunnelPortRange[0] || sForwardAddInfo.RemotePort > sForwardServerStore.TunnelPortRange[1])
                {
                    return $"sforward tunnel port range {string.Join("-", sForwardServerStore.TunnelPortRange)}";
                }
            }
            return await Task.FromResult(string.Empty);
        }
    }
}
