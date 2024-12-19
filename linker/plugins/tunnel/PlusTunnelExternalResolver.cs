using linker.plugins.resolver;
using linker.messenger.tunnel;

namespace linker.plugins.tunnel
{
    /// <summary>
    /// 外网端口处理器
    /// </summary>
    public class PlusTunnelExternalResolver : TunnelExternalResolver, IResolver
    {
        public ResolverType Type => ResolverType.External;

        public PlusTunnelExternalResolver()
        {
        }
    }
}