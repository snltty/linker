using linker.plugins.client;
using linker.tunnel;

namespace linker.plugins.tunnel
{
    public sealed class PlusTunnelMessengerAdapter
    {
        public PlusTunnelMessengerAdapter(ClientSignInState clientSignInState, TunnelConfigTransfer tunnelConfigTransfer, TunnelTransfer tunnelTransfer)
        {
            clientSignInState.NetworkEnabledHandle += (times) => tunnelTransfer.Refresh();
            tunnelConfigTransfer.OnChanged += () => tunnelTransfer.Refresh();
        }
    }
}
