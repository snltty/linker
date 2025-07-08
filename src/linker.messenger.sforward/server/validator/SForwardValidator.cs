using linker.messenger.signin;
using linker.messenger.wlist;

namespace linker.messenger.sforward.server.validator
{
    /// <summary>
    /// 服务端穿透验证
    /// </summary>
    public sealed class SForwardValidator : ISForwardValidator
    {
        public string Name => "default";

        private readonly ISForwardServerStore sForwardServerStore;
        private readonly IWhiteListServerStore whiteListServerStore;
        public SForwardValidator(ISForwardServerStore sForwardServerStore, IWhiteListServerStore whiteListServerStore)
        {
            this.sForwardServerStore = sForwardServerStore;
            this.whiteListServerStore = whiteListServerStore;
        }

        public async Task<string> Validate(SignCacheInfo signCacheInfo, SForwardAddInfo sForwardAddInfo)
        {
            List<string> sforward = await whiteListServerStore.Get("SForward", signCacheInfo.UserId);
            string target = string.IsNullOrWhiteSpace(sForwardAddInfo.Domain) ? sForwardAddInfo.RemotePort.ToString() : sForwardAddInfo.Domain;

            if (signCacheInfo.Super == false && sforward.Contains(target) == false)
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
