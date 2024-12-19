using linker.plugins.resolver;
using linker.libs;
using linker.messenger.relay.server;

namespace linker.plugins.relay.server
{
    /// <summary>
    /// 中继连接处理
    /// </summary>
    public class PlusRelayServerResolver  : RelayServerResolver, IResolver
    {
        public ResolverType Type => ResolverType.Relay;

        public PlusRelayServerResolver(RelayServerNodeTransfer relayServerNodeTransfer,ISerializer serializer):base(relayServerNodeTransfer, serializer)
        {
        }
    }
}