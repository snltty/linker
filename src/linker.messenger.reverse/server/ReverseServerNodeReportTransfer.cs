using linker.libs;
using linker.libs.extends;
using linker.messenger.node;
using linker.messenger.reverse.messenger;

namespace linker.messenger.reverse.server
{
    public sealed class ReverseServerNodeReportTransfer : NodeReportTransfer<ReverseServerConfigInfo, ReverseServerNodeStoreInfo, ReverseServerNodeReportInfo>
    {
        public override ushort MessengerIdSahre => (ushort)ReverseMessengerIds.Share;
        public override ushort MessengerIdUpdate => (ushort)ReverseMessengerIds.Update;
        public override ushort MessengerIdUpgrade => (ushort)ReverseMessengerIds.Upgrade;
        public override ushort MessengerIdExit => (ushort)ReverseMessengerIds.Exit;
        public override ushort MessengerIdReport => (ushort)ReverseMessengerIds.Report;
        public override ushort MessengerIdSignIn => (ushort)ReverseMessengerIds.SignIn;
        public override ushort MessengerIdMasters => (ushort)ReverseMessengerIds.Masters;
        public override ushort MessengerIdDenys => (ushort)ReverseMessengerIds.Denys;
        public override ushort MessengerIdDenysAdd => (ushort)ReverseMessengerIds.DenysAdd;
        public override ushort MessengerIdDenysDel => (ushort)ReverseMessengerIds.DenysDel;

        protected override string Name => "Reverse";

        private readonly IReverseServerWhiteListStore ReverseServerWhiteListStore;
        private readonly IReverseNodeConfigStore nodeConfigStore;
        private readonly IReverseNodeStore nodeStore;
        private readonly ReverseServerConnectionTransfer nodeConnectionTransfer;

        public ReverseServerNodeReportTransfer(IReverseServerWhiteListStore ReverseServerWhiteListStore, ReverseServerConnectionTransfer nodeConnectionTransfer,
            IReverseNodeConfigStore nodeConfigStore,
            ISerializer serializer, IMessengerSender messengerSender, IReverseNodeStore nodeStore,
            IMessengerResolver messengerResolver, ICommonStore commonStore, IReverseServerMasterDenyStore ReverseServerMasterDenyStore)
            : base(nodeConnectionTransfer, nodeConfigStore, serializer, messengerSender, nodeStore, messengerResolver, commonStore, ReverseServerMasterDenyStore)
        {
            this.ReverseServerWhiteListStore = ReverseServerWhiteListStore;
            this.nodeConnectionTransfer = nodeConnectionTransfer;
            this.nodeConfigStore = nodeConfigStore;
            this.nodeStore = nodeStore;
        }

        public override async Task<bool> Update(IConnection conn, ReverseServerNodeStoreInfo info)
        {
            if (nodeConnectionTransfer.TryGet(ConnectionSideType.Master, conn.Id, out var connection) == false || connection.Manageable == false) return false;

            Config.Connections = info.Connections;
            Config.Bandwidth = info.Bandwidth;
            Config.DataEachMonth = info.DataEachMonth;
            Config.DataRemain = info.DataRemain;
            Config.Logo = info.Logo;
            Config.Name = info.Name;
            Config.Url = info.Url;
            Config.Host = info.Host.Split(':')[0];
            Config.Domain = info.Domain;
            Config.WebPort = info.WebPort;
            Config.TunnelPorts = info.TunnelPorts;

            nodeConfigStore.Confirm();

            return true;
        }
        protected override void BuildReport(ReverseServerNodeReportInfo info)
        {
            info.Domain = Config.Domain;
            info.TunnelPorts = Config.TunnelPorts;
            info.WebPort = Config.WebPort;
        }

        /// <summary>
        /// 获取节点列表
        /// </summary>
        /// <param name="super">是否已认证</param>
        /// <returns></returns>
        public override async Task<List<ReverseServerNodeStoreInfo>> GetNodes(bool super, string userid, string machineId)
        {
            List<string> Reverse = (await ReverseServerWhiteListStore.GetNodes(userid, machineId)).Where(c => c.Bandwidth >= 0).SelectMany(c => c.Nodes).ToList();

            var result = (await nodeStore.GetAll())
                .Where(c => super || Environment.TickCount64 - c.LastTicks < 15000)
                .Where(c =>
                {
                    return super || (c.Public && (Reverse.Contains(c.NodeId) || Reverse.Contains("*")));
                })
                .OrderByDescending(c => c.LastTicks);

            var list = result.ThenBy(x => x.BandwidthRatio)
                     .ThenByDescending(x => x.BandwidthEach == 0 ? double.MaxValue : x.BandwidthEach)
                     .ThenByDescending(x => x.Bandwidth == 0 ? double.MaxValue : x.Bandwidth)
                     .ThenByDescending(x => x.DataEachMonth == 0 ? double.MaxValue : x.DataEachMonth)
                     .ThenByDescending(x => x.DataRemain == 0 ? long.MaxValue : x.DataRemain).ToList();
            list = list.ToJson().DeJson<List<ReverseServerNodeStoreInfo>>();
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
