using linker.libs;
using linker.messenger.signin;
using linker.messenger.tunnel.client;
using linker.tunnel;
using linker.tunnel.connection;

namespace linker.messenger.pcp
{
    public sealed class PcpNodeTransfer
    {
        private readonly SignInClientTransfer signInClientTransfer;
        private readonly IPcpStore pcpStore;
        private readonly TunnelDecenter tunnelDecenter;
        private readonly IMessengerSender messengerSender;
        private readonly ISerializer serializer;
        private readonly SignInClientState signInClientState;


        public PcpNodeTransfer(SignInClientTransfer signInClientTransfer, IPcpStore pcpStore,
            TunnelDecenter tunnelDecenter, IMessengerSender messengerSender, ISerializer serializer,
            SignInClientState signInClientState, TunnelTransfer tunnelTransfer)
        {
            this.signInClientTransfer = signInClientTransfer;
            this.pcpStore = pcpStore;
            this.tunnelDecenter = tunnelDecenter;
            this.messengerSender = messengerSender;
            this.serializer = serializer;
            this.signInClientState = signInClientState;

            tunnelTransfer.SetConnectedCallback(Helper.GlobalString, OnConnected);
        }
        private void OnConnected(ITunnelConnection connection)
        {
            pcpStore.AddHistory(connection);
        }

        public void RemoveNodes(List<string> nodeIds)
        {
            pcpStore.RemoveHistorys(nodeIds);
        }
        public async Task<List<string>> GetNodeIds(string machineId, string nodeId)
        {
            return (await GetNodes(machineId, nodeId).ConfigureAwait(false)).Where(c => c.Enabled).Select(n => n.NodeId).ToList();
        }
        public async Task<List<PcpNodeInfo>> GetNodes(string machineId, string nodeId)
        {
            List<string> offlines = await signInClientTransfer.GetOfflines(pcpStore.PcpHistory.History).ConfigureAwait(false);
            List<string> remoteNodes = await GetRemoteNodes(machineId).ConfigureAwait(false);
            List<string> nodes = pcpStore.PcpHistory.History.Intersect(remoteNodes).Except(offlines).ToList();

            var tunnelConfigs = tunnelDecenter.Config.Select(c => (c.Value.MachineId, c.Value.Relay)).ToList();

            var result = nodes.Join(tunnelConfigs, n => n, tc => tc.MachineId, (n, tc) => new PcpNodeInfo { NodeId = n, Enabled = tc.Relay.Enabled, Bandwidth = tc.Relay.Bandwidth })
                .OrderByDescending(c => c.Enabled)
                .OrderByDescending(c => c.Bandwidth == 0 ? int.MaxValue : c.Bandwidth);

            return result.Where(c => c.NodeId == nodeId)
                .Concat(result.Where(c => c.NodeId != nodeId)).ToList();
        }
        private async Task<List<string>> GetRemoteNodes(string machineId)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)PcpMessengerIds.NodesForward,
                Payload = serializer.Serialize(machineId),
                Timeout = 5000,
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK)
            {
                return serializer.Deserialize<List<string>>(resp.Data.Span);
            }
            return [];
        }


    }
    public sealed class PcpNodeInfo
    {
        public string NodeId { get; set; }
        public string NodeName { get; set; }
        public bool Enabled { get; set; }
        public int Bandwidth { get; set; }
    }
}
