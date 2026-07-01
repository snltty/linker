
using linker.libs.extends;
using linker.libs.web;
using linker.tunnel;
using linker.tunnel.connection;

namespace linker.messenger.mesh
{
    public sealed class MeshApiController : IApiController
    {
        private readonly TunnelTransfer tunnelTransfer;
        private readonly MeshNodeTransfer meshHistoryTransfer;

        public MeshApiController(TunnelTransfer tunnelTransfer, MeshNodeTransfer meshHistoryTransfer)
        {
            this.tunnelTransfer = tunnelTransfer;
            this.meshHistoryTransfer = meshHistoryTransfer;
        }
        public bool Connect(ApiControllerParamsInfo param)
        {
            MeshConnectInfo info = param.Content.DeJson<MeshConnectInfo>();
            info.Configures["flag"] = "mesh";
            info.Configures["mesh"] = new MeshInfo { NodeId = info.NodeId }.ToJson();
            _ = tunnelTransfer.ConnectAsync(info.ToMachineId, info.TransactionId, info.Configures, tunnelTypes: [TunnelType.Mesh]);
            return true;
        }
        public Task<List<MeshNodeInfo>> GetNodes(ApiControllerParamsInfo param)
        {
            return meshHistoryTransfer.GetNodes(param.Content, string.Empty);
        }
        public bool DelNodes(ApiControllerParamsInfo param)
        {
            meshHistoryTransfer.RemoveNodes(param.Content.DeJson<List<string>>());
            return true;
        }

    }

    public sealed class MeshConnectInfo
    {
        public string ToMachineId { get; set; }
        public string TransactionId { get; set; }
        public string NodeId { get; set; }
        public Dictionary<string, string> Configures { get; set; } = [];
    }
}
