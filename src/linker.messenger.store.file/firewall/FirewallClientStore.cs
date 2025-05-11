using linker.messenger.firewall;
using LiteDB;

namespace linker.messenger.store.file.firewall
{

    public sealed class FirewallClientStore : IFirewallClientStore
    {
        private readonly Storefactory dBfactory;
        private readonly ILiteCollection<FirewallRuleInfo> liteCollection;
        private readonly RunningConfig runningConfig;
        public FirewallClientStore(Storefactory dBfactory, RunningConfig runningConfig)
        {
            this.dBfactory = dBfactory;
            liteCollection = dBfactory.GetCollection<FirewallRuleInfo>("firewall");
            this.runningConfig = runningConfig;
        }

        public snat.LinkerFirewallState State => runningConfig.Data.Firewall.State;
        public void SetState(snat.LinkerFirewallState state)
        {
            runningConfig.Data.Firewall.State = state;
            runningConfig.Data.Update();
        }

        public IEnumerable<FirewallRuleInfo> GetAll(FirewallSearchInfo info)
        {
            IEnumerable<FirewallRuleInfo> list = liteCollection.FindAll()
                .Where(c => c.GroupId == info.GroupId)
                .Where(c => (c.Protocol & info.Protocol) > 0)
                .Where(c => (c.Action & info.Action) > 0);

            if (info.Disabled != -1)
            {
                list = list.Where(c => c.Disabled == Convert.ToBoolean(info.Disabled));
            }

            if (string.IsNullOrWhiteSpace(info.Str) == false)
            {
                list = list.Where(c =>
                (c.SrcName != null && c.SrcName.Contains(info.Str)) ||
                (c.DstCIDR != null && c.DstCIDR.Contains(info.Str)) ||
                (c.DstPort != null && c.DstPort.Contains(info.Str)) ||
                (c.Remark != null && c.Remark.Contains(info.Str))
                );
            }

            return list.OrderBy(c => c.OrderBy);
        }

        public IEnumerable<FirewallRuleInfo> GetEnabled(string groupId)
        {
            return liteCollection.FindAll().Where(c => c.Disabled == false && c.GroupId == groupId).OrderBy(c => c.OrderBy);
        }

        public bool Add(FirewallRuleInfo rule)
        {
            if (string.IsNullOrWhiteSpace(rule.Id))
            {
                rule.Id = ObjectId.NewObjectId().ToString();
                return liteCollection.Insert(rule) != null;
            }
            else
            {
                return liteCollection.UpdateMany(p => new FirewallRuleInfo
                {
                    SrcId = rule.SrcId,
                    SrcName = rule.SrcName,
                    GroupId = rule.GroupId,
                    DstCIDR = rule.DstCIDR,
                    DstPort = rule.DstPort,
                    Protocol = rule.Protocol,
                    Action = rule.Action,
                    OrderBy = rule.OrderBy,
                    Disabled = rule.Disabled,
                    Remark = rule.Remark
                }, c => c.Id == rule.Id) > 0;
            }
        }

        public bool Remove(string id)
        {
            return liteCollection.Delete(id);
        }
    }
}
