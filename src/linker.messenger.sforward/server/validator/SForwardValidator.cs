using linker.messenger.node;
using linker.messenger.signin;

namespace linker.messenger.sforward.server.validator
{
    /// <summary>
    /// 服务端穿透验证
    /// </summary>
    public sealed class SForwardValidator : ISForwardValidator
    {
        public string Name => "default";

        private readonly ISForwardNodeConfigStore nodeConfigStore;
        public SForwardValidator(ISForwardNodeConfigStore nodeConfigStore)
        {
            this.nodeConfigStore = nodeConfigStore;
        }

        public async Task<string> Validate(SignCacheInfo signCacheInfo, SForwardAddInfo sForwardAddInfo)
        {
            if (ValidatePort(sForwardAddInfo.RemotePort) == false)
            {
                return $"port out of range";
            }
            return await Task.FromResult(string.Empty);
        }
        private bool ValidatePort(int port)
        {
            if (port <= 0)
            {
                return true;
            }

            string ports = nodeConfigStore.Config.TunnelPorts;
            return string.IsNullOrWhiteSpace(ports)
                || $",{ports},".Contains($",{port},")
                || ports.Split(',').Where(c => c.Contains('-')).Any(c =>
            {
                try
                {
                    int[] p = c.Split('-').Select(c => int.Parse(c)).ToArray();
                    return p.Length == 2 && port >= p[0] && port <= p[1];
                }
                catch (Exception)
                {
                }
                return false;
            });
        }
    }
}
