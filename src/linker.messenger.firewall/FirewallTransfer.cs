using linker.messenger.signin;
using linker.nat;

namespace linker.messenger.firewall
{
    public sealed class FirewallTransfer
    {
        private readonly IFirewallClientStore firewallClientStore;
        private readonly ISignInClientStore signInClientStore;
        private readonly LinkerFirewall linkerFirewall;

        public FirewallTransfer(IFirewallClientStore firewallClientStore, SignInClientState signInClientState, ISignInClientStore signInClientStore, LinkerFirewall linkerFirewall)
        {
            this.firewallClientStore = firewallClientStore;
            this.signInClientStore = signInClientStore;
            this.linkerFirewall = linkerFirewall;

            signInClientState.OnSignInSuccess += Reset;
        }
        private void Reset(int times)
        {
            BuildRules();
        }
        private void BuildRules()
        {
            linkerFirewall.BuildRules(firewallClientStore.GetEnabled(signInClientStore.Group.Id).Select(c => (LinkerFirewallRuleInfo)c).ToList());
            linkerFirewall.SetState(firewallClientStore.State);
        }

        public bool State(LinkerFirewallState state)
        {
            firewallClientStore.SetState(state);
            linkerFirewall.SetState(state);
            BuildRules();
            return true;
        }
        public bool Check(FirewallCheckInfo info)
        {
            return firewallClientStore.Check(info);
        }

        public List<FirewallRuleInfo> Get()
        {
            return firewallClientStore.GetAll().ToList();
        }
        public FirewallListInfo Get(FirewallSearchInfo info)
        {
            return new FirewallListInfo
            {
                List = firewallClientStore.GetAll(info).ToList(),
                State = firewallClientStore.State
            };
        }
        public bool Add(FirewallRuleInfo info)
        {
            info.GroupId = signInClientStore.Group.Id;
            firewallClientStore.Add(info);
            BuildRules();
            return true;
        }
        public bool Add(List<FirewallRuleInfo> infos)
        {
            foreach (var item in infos)
            {
                item.GroupId = signInClientStore.Group.Id;
            }

            firewallClientStore.Add(infos);
            BuildRules();
            return true;
        }
        public bool Remove(string id)
        {
            firewallClientStore.Remove(id);
            BuildRules();
            return true;
        }
        public bool Remove(List<string> ids)
        {
            firewallClientStore.Remove(ids);
            BuildRules();
            return true;
        }

    }
}
