using linker.nat;

namespace linker.messenger.store.file
{
    public sealed partial class RunningConfigInfo
    {
        public FirewallConfigInfo Firewall { get; set; } = new FirewallConfigInfo();
    }

    public sealed class FirewallConfigInfo
    {
        public LinkerFirewallState State { get; set; } = LinkerFirewallState.Disabled;
    }

}
