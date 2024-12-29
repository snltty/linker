using linker.libs;
using linker.messenger.sync;
using linker.plugins.tunnel;
using linker.tunnel.transport;

namespace linker.messenger.tunnel
{
    public sealed class TunnelSyncTransports : ISync
    {
        public string Name => "TunnelTransports";

        private readonly ITunnelClientStore tunnelClientStore;
        private readonly ISerializer serializer;
        public TunnelSyncTransports(ITunnelClientStore tunnelClientStore, ISerializer serializer)
        {
            this.tunnelClientStore = tunnelClientStore;
            this.serializer = serializer;
        }
        public Memory<byte> GetData()
        {
            return serializer.Serialize(tunnelClientStore.GetTunnelTransports().Result);
        }

        public void SetData(Memory<byte> data)
        {
            tunnelClientStore.SetTunnelTransports(serializer.Deserialize<List<TunnelTransportItemInfo>>(data.Span));
        }
    }
}
