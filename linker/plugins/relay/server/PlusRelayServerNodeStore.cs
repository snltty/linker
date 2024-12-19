using linker.messenger.relay.server;
using linker.plugins.resolver;
using linker.plugins.server;

namespace linker.plugins.relay.server
{
    public sealed class PlusRelayServerNodeStore : IRelayServerNodeStore
    {
        public byte Flag => (byte)ResolverType.RelayReport;

        public int ServicePort => serverConfigTransfer.Port;

        public RelayServerNodeInfo Node => relayServerConfigTransfer.Node;

        private readonly RelayServerConfigTransfer relayServerConfigTransfer;
        private readonly ServerConfigTransfer serverConfigTransfer;
        public PlusRelayServerNodeStore(RelayServerConfigTransfer relayServerConfigTransfer, ServerConfigTransfer serverConfigTransfer)
        {
            this.relayServerConfigTransfer = relayServerConfigTransfer;
            this.serverConfigTransfer = serverConfigTransfer;
        }

        public void Confirm()
        {
            relayServerConfigTransfer.Update();
        }

        public void SetMaxGbTotalLastBytes(ulong value)
        {
            relayServerConfigTransfer.SetMaxGbTotalLastBytes(value);
        }

        public void SetMaxGbTotalMonth(int month)
        {
            relayServerConfigTransfer.SetMaxGbTotalMonth(month);
        }
    }
}
