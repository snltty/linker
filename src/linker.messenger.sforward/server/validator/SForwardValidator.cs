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
        private readonly SForwardServerMasterTransfer sForwardServerMasterTransfer;
        private readonly ISForwardServerNodeStore sForwardServerNodeStore;
        public SForwardValidator(ISForwardServerStore sForwardServerStore, SForwardServerMasterTransfer sForwardServerMasterTransfer, ISForwardServerNodeStore sForwardServerNodeStore)
        {
            this.sForwardServerStore = sForwardServerStore;
            this.sForwardServerMasterTransfer = sForwardServerMasterTransfer;
            this.sForwardServerNodeStore = sForwardServerNodeStore;
        }

        public async Task<string> Validate(SignCacheInfo signCacheInfo, SForwardAddInfo sForwardAddInfo)
        {
            if (string.IsNullOrWhiteSpace(sForwardAddInfo.NodeId)) sForwardAddInfo.NodeId = sForwardServerNodeStore.Node.Id;

            if (sForwardAddInfo.RemotePort > 0)
            {
                if (sForwardAddInfo.RemotePort < sForwardServerStore.TunnelPortRange[0] || sForwardAddInfo.RemotePort > sForwardServerStore.TunnelPortRange[1])
                {
                    return $"port out of range {string.Join("-", sForwardServerStore.TunnelPortRange)}";
                }
            }
            return await Task.FromResult(string.Empty);
        }
    }
}
