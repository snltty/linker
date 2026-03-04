using linker.libs;
using linker.messenger.sync;
using linker.tunnel;
using linker.tunnel.transport;

namespace linker.messenger.tunnel.client
{
    public sealed class TunnelSyncTransports : ISync
    {
        public string Name => "TunnelTransports";

        private readonly ISerializer serializer;
        private readonly ITunnelMessengerAdapter tunnelMessengerAdapter;
        public TunnelSyncTransports(ISerializer serializer, ITunnelMessengerAdapter tunnelMessengerAdapter)
        {
            this.serializer = serializer;
            this.tunnelMessengerAdapter = tunnelMessengerAdapter;
        }
        public Memory<byte> GetData()
        {
            return serializer.Serialize(tunnelMessengerAdapter.GetTunnelTransports(Helper.GlobalString).Result);
        }

        public void SetData(Memory<byte> data)
        {
            tunnelMessengerAdapter.SetTunnelTransports(Helper.GlobalString, serializer.Deserialize<List<TunnelTransportItemInfo>>(data.Span));
        }
    }
}
