
using linker.messenger.node;
using linker.messenger.signin;

namespace linker.messenger.relay.server
{
    /// <summary>
    /// 中继主机操作
    /// </summary>
    public class RelayServerMasterTransfer
    {
        private readonly IRelayServerCaching relayCaching;
        private readonly IRelayServerWhiteListStore relayServerWhiteListStore;
        private readonly RelayServerConnectionTransfer relayServerConnectionTransfer;
        public RelayServerMasterTransfer(IRelayServerCaching relayCaching, IRelayServerWhiteListStore relayServerWhiteListStore,
            RelayServerConnectionTransfer relayServerConnectionTransfer)
        {
            this.relayCaching = relayCaching;
            this.relayServerWhiteListStore = relayServerWhiteListStore;
            this.relayServerConnectionTransfer = relayServerConnectionTransfer;
        }

        public bool AddRelay(SignCacheInfo from, SignCacheInfo to, uint flowid)
        {
            RelayCacheInfo cache = new RelayCacheInfo
            {
                FlowId = flowid,
                FromId = from.MachineId,
                FromName = from.MachineName,
                ToId = to.Id,
                ToName = to.MachineName,
                GroupId = to.GroupId,
                Super = from.Super,
                UserId = from.UserId,
            };
            return relayCaching.TryAdd($"{cache.FromId}->{cache.ToId}->{flowid}", cache, 15000);
        }
        public async Task<RelayCacheInfo> TryGetRelayCache(string key, string nodeid)
        {
            if (relayServerConnectionTransfer.TryGet(ConnectionSideType.Node, nodeid, out ConnectionInfo connection) == false)
            {
                return null;
            }

            if (relayCaching.TryGetValue(key, out RelayCacheInfo cache))
            {
                List<double> bandwidth = await relayServerWhiteListStore.GetBandwidth(cache.UserId, cache.FromId, cache.ToId, nodeid);
                if (bandwidth.Any(c => c < 0))
                {
                    return null;
                }

                cache.Bandwidth = bandwidth.Count > 0
                ? bandwidth.Any(c => c == 0) ? 0 : bandwidth.Min()
                : cache.Super ? 0 : -1;

                return cache;
            }
            return null;
        }

    }
}
