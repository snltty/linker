using linker.libs;
using linker.messenger.sync;
using linker.tunnel;
using linker.tunnel.transport;

namespace linker.messenger.tunnel
{
    public sealed class TunnelSyncTransports : ISync
    {
        public string Name => "TunnelTransports";

        private readonly ITunnelClientStore tunnelClientStore;
        private readonly ISerializer serializer;
        private readonly ITunnelMessengerAdapter tunnelMessengerAdapter;
        public TunnelSyncTransports(ITunnelClientStore tunnelClientStore, ISerializer serializer, ITunnelMessengerAdapter tunnelMessengerAdapter)
        {
            this.tunnelClientStore = tunnelClientStore;
            this.serializer = serializer;
            this.tunnelMessengerAdapter = tunnelMessengerAdapter;
        }
        public Memory<byte> GetData()
        {
            return serializer.Serialize(tunnelMessengerAdapter.GetTunnelTransports("default").Result);
        }

        public void SetData(Memory<byte> data)
        {
            tunnelMessengerAdapter.SetTunnelTransports("default", serializer.Deserialize<List<TunnelTransportItemInfo>>(data.Span));
        }
    }
}
