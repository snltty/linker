using linker.plugins.config;
using linker.tunnel.transport;
using MemoryPack;

namespace linker.plugins.tunnel
{
    public sealed class TunnelConfigSyncTransports : IConfigSync
    {
        public string Name => "TunnelTransports";

        private readonly TunnelConfigTransfer tunnelConfigTransfer;
        public TunnelConfigSyncTransports( TunnelConfigTransfer tunnelConfigTransfer)
        {
            this.tunnelConfigTransfer = tunnelConfigTransfer;
        }
        public Memory<byte> GetData()
        {
            return MemoryPackSerializer.Serialize(tunnelConfigTransfer.Transports);
        }

        public void SetData(Memory<byte> data)
        {
            tunnelConfigTransfer.SetTransports(MemoryPackSerializer.Deserialize<List<TunnelTransportItemInfo>>(data.Span));
        }
    }
}
