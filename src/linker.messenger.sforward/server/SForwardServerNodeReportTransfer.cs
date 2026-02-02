using linker.libs;
using linker.libs.extends;
using linker.messenger.node;
using linker.messenger.sforward.messenger;

namespace linker.messenger.sforward.server
{
    public sealed class SForwardServerNodeReportTransfer : NodeReportTransfer<SForwardServerConfigInfo, SForwardServerNodeStoreInfo, SForwardServerNodeReportInfo>
    {
        public override ushort MessengerIdSahre => (ushort)SForwardMessengerIds.Share;
        public override ushort MessengerIdUpdate => (ushort)SForwardMessengerIds.Update;
        public override ushort MessengerIdUpgrade => (ushort)SForwardMessengerIds.Upgrade;
        public override ushort MessengerIdExit => (ushort)SForwardMessengerIds.Exit;
        public override ushort MessengerIdReport => (ushort)SForwardMessengerIds.Report;
        public override ushort MessengerIdSignIn => (ushort)SForwardMessengerIds.SignIn;
        public override ushort MessengerIdMasters => (ushort)SForwardMessengerIds.Masters;
        public override ushort MessengerIdDenys => (ushort)SForwardMessengerIds.Denys;
        public override ushort MessengerIdDenysAdd => (ushort)SForwardMessengerIds.DenysAdd;
        public override ushort MessengerIdDenysDel => (ushort)SForwardMessengerIds.DenysDel;

        protected override string Name => "sforward";

        private readonly ISForwardServerWhiteListStore sforwardServerWhiteListStore;
        private readonly ISForwardNodeConfigStore nodeConfigStore;
        private readonly ISForwardNodeStore nodeStore;
        private readonly SForwardServerConnectionTransfer nodeConnectionTransfer;

        public SForwardServerNodeReportTransfer(ISForwardServerWhiteListStore sforwardServerWhiteListStore, SForwardServerConnectionTransfer nodeConnectionTransfer,
            ISForwardNodeConfigStore nodeConfigStore,
            ISerializer serializer, IMessengerSender messengerSender, ISForwardNodeStore nodeStore,
            IMessengerResolver messengerResolver, ICommonStore commonStore, ISForwardServerMasterDenyStore sforwardServerMasterDenyStore)
            : base(nodeConnectionTransfer, nodeConfigStore, serializer, messengerSender, nodeStore, messengerResolver, commonStore, sforwardServerMasterDenyStore)
        {
            this.sforwardServerWhiteListStore = sforwardServerWhiteListStore;
            this.nodeConnectionTransfer = nodeConnectionTransfer;
            this.nodeConfigStore = nodeConfigStore;
            this.nodeStore = nodeStore;
        }

        public override async Task<bool> Update(IConnection conn, SForwardServerNodeStoreInfo info)
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
        protected override void BuildReport(SForwardServerNodeReportInfo info)
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
        public override async Task<List<SForwardServerNodeStoreInfo>> GetNodes(bool super, string userid, string machineId)
        {
            List<string> sforward = (await sforwardServerWhiteListStore.GetNodes(userid, machineId)).Where(c => c.Bandwidth >= 0).SelectMany(c => c.Nodes).ToList();

            var result = (await nodeStore.GetAll())
                .Where(c => super || Environment.TickCount64 - c.LastTicks < 15000)
                .Where(c =>
                {
                    return super || (c.Public && (sforward.Contains(c.NodeId) || sforward.Contains("*")));
                })
                .OrderByDescending(c => c.LastTicks);

            var list = result.ThenBy(x => x.BandwidthRatio)
                     .ThenByDescending(x => x.BandwidthEach == 0 ? double.MaxValue : x.BandwidthEach)
                     .ThenByDescending(x => x.Bandwidth == 0 ? double.MaxValue : x.Bandwidth)
                     .ThenByDescending(x => x.DataEachMonth == 0 ? double.MaxValue : x.DataEachMonth)
                     .ThenByDescending(x => x.DataRemain == 0 ? long.MaxValue : x.DataRemain).ToList();
            list = list.ToJson().DeJson<List<SForwardServerNodeStoreInfo>>();
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
