using linker.messenger.relay.server;
using linker.plugins.resolver;

namespace linker.plugins.relay.server
{
    public class PlusRelayServerReportResolver : RelayServerReportResolver, IResolver
    {
        public ResolverType Type => ResolverType.RelayReport;

        public PlusRelayServerReportResolver(RelayServerMasterTransfer relayServerTransfer):base(relayServerTransfer)
        {
        }

    }
}
