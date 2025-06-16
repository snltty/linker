using linker.libs;
using linker.messenger.sync;

namespace linker.messenger.firewall
{
    public sealed class FirewallSync : ISync
    {
        public string Name => "Firewall";

        private readonly FirewallTransfer firewallTransfer;
        private readonly ISerializer serializer;
        public FirewallSync(FirewallTransfer firewallTransfer, ISerializer serializer)
        {
            this.firewallTransfer = firewallTransfer;
            this.serializer = serializer;
        }
        public Memory<byte> GetData()
        {
            return serializer.Serialize(firewallTransfer.Get().Where(c => c.Checked).ToList());
        }

        public void SetData(Memory<byte> data)
        {
            List<string> ids = firewallTransfer.Get().Select(c => c.Id).ToList();

            List<FirewallRuleInfo> list = serializer.Deserialize<List<FirewallRuleInfo>>(data.Span);
            foreach (FirewallRuleInfo rule in list)
            {
                rule.Id = string.Empty;
            }
            firewallTransfer.Add(list);

            firewallTransfer.Remove(ids);
        }
    }
}
