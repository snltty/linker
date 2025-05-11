using linker.snat;

namespace linker.messenger.firewall
{
    public interface IFirewallClientStore
    {
        public LinkerFirewallState State { get; }
        public void SetState(LinkerFirewallState state);

        public IEnumerable<FirewallRuleInfo> GetAll(FirewallSearchInfo searchInfo);
        public IEnumerable<FirewallRuleInfo> GetEnabled(string groupId);
        public bool Add(FirewallRuleInfo rule);
        public bool Remove(string id);
    }

    public sealed class FirewallRuleInfo : linker.snat.LinkerFirewallRuleInfo
    {
        public string Id { get; set; }
        public string GroupId { get; set; }
        public string SrcName { get; set; }
        public bool Disabled { get; set; }
        public int OrderBy { get; set; }
        public string Remark { get; set; }
    }


    public sealed partial class FirewallSearchForwardInfo
    {
        public string MachineId { get; set; }
        public FirewallSearchInfo Data { get; set; }
    }
    public sealed partial class FirewallSearchInfo
    {
        public string GroupId { get; set; }
        public string Str { get; set; }
        public int Disabled { get; set; }
        public LinkerFirewallProtocolType Protocol { get; set; }
        public LinkerFirewallAction Action { get; set; }
    }
    public sealed partial class FirewallListInfo
    {
        public LinkerFirewallState State { get; set; } = LinkerFirewallState.Disabled;
        public List<FirewallRuleInfo> List { get; set; } = new List<FirewallRuleInfo>();
    }

    public sealed partial class FirewallAddForwardInfo
    {
        public string MachineId { get; set; }
        public FirewallRuleInfo Data { get; set; }
    }
    public sealed partial class FirewallRemoveForwardInfo
    {
        public string MachineId { get; set; }
        public string Id { get; set; }
    }

    public sealed partial class FirewallStateForwardInfo
    {
        public string MachineId { get; set; }
        public LinkerFirewallState State { get; set; }
    }
}
