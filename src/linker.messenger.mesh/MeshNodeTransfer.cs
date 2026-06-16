using linker.libs;
using linker.messenger.signin;
using linker.messenger.tunnel.client;
using linker.tunnel;
using linker.tunnel.connection;
using linker.tunnel.transport;

namespace linker.messenger.mesh
{
    public sealed class MeshNodeTransfer
    {
        private readonly SignInClientTransfer signInClientTransfer;
        private readonly IMeshStore meshStore;
        private readonly TunnelDecenter tunnelDecenter;
        private readonly IMessengerSender messengerSender;
        private readonly ISerializer serializer;
        private readonly SignInClientState signInClientState;


        public MeshNodeTransfer(SignInClientTransfer signInClientTransfer, IMeshStore meshStore,
            TunnelDecenter tunnelDecenter, IMessengerSender messengerSender, ISerializer serializer,
            SignInClientState signInClientState, TunnelTransfer tunnelTransfer)
        {
            this.signInClientTransfer = signInClientTransfer;
            this.meshStore = meshStore;
            this.tunnelDecenter = tunnelDecenter;
            this.messengerSender = messengerSender;
            this.serializer = serializer;
            this.signInClientState = signInClientState;

            tunnelTransfer.SetConnectedCallback(Helper.GlobalString, OnConnected);
        }
        private void OnConnected(ITunnelConnection connection, TunnelTransportInfo info)
        {
            meshStore.AddHistory(connection);
        }

        public void RemoveNodes(List<string> nodeIds)
        {
            meshStore.RemoveHistorys(nodeIds);
        }
        public async Task<List<string>> GetNodeIds(string machineId, string nodeId)
        {
            return (await GetNodes(machineId, nodeId).ConfigureAwait(false)).Where(c => c.Enabled).Select(n => n.NodeId).ToList();
        }
        public async Task<List<MeshNodeInfo>> GetNodes(string machineId, string nodeId)
        {
            List<string> offlines = await signInClientTransfer.GetOfflines(meshStore.MeshHistory.History).ConfigureAwait(false);
            List<string> remoteNodes = await GetRemoteNodes(machineId).ConfigureAwait(false);
            List<string> nodes = meshStore.MeshHistory.History.Intersect(remoteNodes).Except(offlines).ToList();

            var tunnelConfigs = tunnelDecenter.Config.Select(c => (c.Value.MachineId, c.Value.Mesh)).ToList();

            var result = nodes.Join(tunnelConfigs, n => n, tc => tc.MachineId, (n, tc) => new MeshNodeInfo { NodeName=tc.Mesh.MachineName, NodeId = n, Enabled = tc.Mesh.Enabled, Bandwidth = tc.Mesh.Bandwidth })
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
                MessengerId = (ushort)MeshMessengerIds.NodesForward,
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
    public sealed class MeshNodeInfo
    {
        public string NodeId { get; set; }
        public string NodeName { get; set; }
        public bool Enabled { get; set; }
        public int Bandwidth { get; set; }
    }
}
