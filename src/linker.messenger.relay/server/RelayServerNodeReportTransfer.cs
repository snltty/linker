using linker.libs;
using linker.libs.extends;
using linker.messenger.node;
using linker.messenger.relay.messenger;

namespace linker.messenger.relay.server
{
    public sealed class RelayServerNodeReportTransfer : NodeReportTransfer<RelayServerConfigInfo, RelayServerNodeStoreInfo, RelayServerNodeReportInfo>
    {
        public override ushort MessengerIdSahre => (ushort)RelayMessengerIds.Share;
        public override ushort MessengerIdUpdate => (ushort)RelayMessengerIds.Update;
        public override ushort MessengerIdUpgrade => (ushort)RelayMessengerIds.Upgrade;
        public override ushort MessengerIdExit => (ushort)RelayMessengerIds.Exit;
        public override ushort MessengerIdReport => (ushort)RelayMessengerIds.Report;
        public override ushort MessengerIdSignIn => (ushort)RelayMessengerIds.SignIn;
        public override ushort MessengerIdMasters => (ushort)RelayMessengerIds.Masters;
        public override ushort MessengerIdDenys => (ushort)RelayMessengerIds.Denys;
        public override ushort MessengerIdDenysAdd => (ushort)RelayMessengerIds.DenysAdd;
        public override ushort MessengerIdDenysDel => (ushort)RelayMessengerIds.DenysDel;

        protected override string Name => "relay";

        private readonly IRelayServerWhiteListStore relayServerWhiteListStore;
        private readonly IRelayNodeConfigStore nodeConfigStore;
        private readonly IRelayNodeStore nodeStore;
        private readonly RelayServerConnectionTransfer nodeConnectionTransfer;

        public RelayServerNodeReportTransfer(IRelayServerWhiteListStore relayServerWhiteListStore, RelayServerConnectionTransfer nodeConnectionTransfer,
            IRelayNodeConfigStore nodeConfigStore,
            ISerializer serializer, IMessengerSender messengerSender, IRelayNodeStore nodeStore,
            IMessengerResolver messengerResolver, ICommonStore commonStore, IRelayServerMasterDenyStore relayServerMasterDenyStore)
            : base(nodeConnectionTransfer, nodeConfigStore, serializer, messengerSender, nodeStore, messengerResolver, commonStore, relayServerMasterDenyStore)
        {
            this.relayServerWhiteListStore = relayServerWhiteListStore;
            this.nodeConnectionTransfer = nodeConnectionTransfer;
            this.nodeConfigStore = nodeConfigStore;
            this.nodeStore = nodeStore;
        }

        public override async Task<bool> Update(IConnection conn, RelayServerNodeStoreInfo info)
        {
            if (nodeConnectionTransfer.TryGet(ConnectionSideType.Master, conn.Id, out var connection) == false || connection.Manageable == false)
            {
                return false;
            }

            Config.Connections = info.Connections;
            Config.Bandwidth = info.Bandwidth;
            Config.Protocol = info.Protocol;
            Config.DataEachMonth = info.DataEachMonth;
            Config.DataRemain = info.DataRemain;
            Config.Logo = info.Logo;
            Config.Name = info.Name;
            Config.Url = info.Url;
            Config.Host = info.Host.Split(':')[0];

            nodeConfigStore.Confirm();

            return true;
        }
        protected override void BuildReport(RelayServerNodeReportInfo info)
        {

        }

        public override async Task<List<RelayServerNodeStoreInfo>> GetNodes(bool super, string userid, string machineId)
        {
            var nodes = (await relayServerWhiteListStore.GetNodes(userid, machineId)).Where(c => c.Bandwidth >= 0).SelectMany(c => c.Nodes);

            var result = (await nodeStore.GetAll())
                .Where(c => super || Environment.TickCount64 - c.LastTicks < 15000)
                .Where(c =>
                {
                    return super || (c.Public && (nodes.Contains(c.NodeId) || nodes.Contains("*")))
                    || (c.Public && c.ConnectionsRatio < c.Connections && (c.DataEachMonth == 0 || (c.DataEachMonth > 0 && c.DataRemain > 0)));
                })
                .OrderByDescending(c => c.LastTicks);

            var list = result.OrderByDescending(x => x.Connections == 0 ? int.MaxValue : x.Connections)

                     .ThenBy(x => x.ConnectionsRatio)
                     .ThenBy(x => x.BandwidthRatio)
                     .ThenByDescending(x => x.BandwidthEach == 0 ? int.MaxValue : x.BandwidthEach)
                     .ThenByDescending(x => x.Bandwidth == 0 ? int.MaxValue : x.Bandwidth)
                     .ThenByDescending(x => x.DataEachMonth == 0 ? int.MaxValue : x.DataEachMonth)
                     .ThenByDescending(x => x.DataRemain == 0 ? long.MaxValue : x.DataRemain)

                     .ToList();

            list = list.ToJson().DeJson<List<RelayServerNodeStoreInfo>>();
            list.ForEach(c =>
            {
                c.ShareKey = string.Empty;
                c.MasterKey = string.Empty;
                c.LastTicks = Math.Abs(Environment.TickCount64 - c.LastTicks);
            });
            return list;
        }


        public async Task<List<RelayServerNodeStoreInfo>> GetPublicNodes()
        {
            var result = (await nodeStore.GetAll())
                .Where(c => Environment.TickCount64 - c.LastTicks < 15000)
                .Where(c => c.Public)
                .OrderByDescending(c => c.LastTicks);

            var list = result.OrderByDescending(x => x.Connections == 0 ? int.MaxValue : x.Connections)
                     .ThenBy(x => x.ConnectionsRatio)
                     .ThenBy(x => x.BandwidthRatio)
                     .ThenByDescending(x => x.BandwidthEach == 0 ? int.MaxValue : x.BandwidthEach)
                     .ThenByDescending(x => x.Bandwidth == 0 ? int.MaxValue : x.Bandwidth)
                     .ThenByDescending(x => x.DataEachMonth == 0 ? int.MaxValue : x.DataEachMonth)
                     .ThenByDescending(x => x.DataRemain == 0 ? long.MaxValue : x.DataRemain)
                     .ToList();

            list = list.ToJson().DeJson<List<RelayServerNodeStoreInfo>>();
            list.ForEach(c =>
            {
                c.ShareKey = string.Empty;
                c.MasterKey = string.Empty;
                c.LastTicks = Math.Abs(Environment.TickCount64 - c.LastTicks);
            });
            return list;
        }
    }
}
