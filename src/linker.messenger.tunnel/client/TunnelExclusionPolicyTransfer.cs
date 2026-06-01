using linker.tunnel;

namespace linker.messenger.tunnel.client
{
    public interface ITunnelExclusionPolicy
    {
        public List<TunnelExclusionPolicyInfo> Query();
    }
    public sealed class TunnelExclusionPolicyTransfer
    {
        private List<ITunnelExclusionPolicy> policys = new List<ITunnelExclusionPolicy>();

        public TunnelExclusionPolicyTransfer()
        {
        }
        public void AddTunnelExclusionPolicyTransfers(List<ITunnelExclusionPolicy> list)
        {
            policys = policys.Concat(list).ToList();
        }

        public List<TunnelExclusionPolicyInfo> Query()
        {
            return policys.SelectMany(c => c.Query()).ToList();
        }
    }

   
}
