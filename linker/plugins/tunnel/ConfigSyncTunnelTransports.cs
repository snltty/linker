using linker.config;
using linker.plugins.config;
using linker.tunnel.transport;
using MemoryPack;

namespace linker.plugins.tunnel
{
    public sealed class ConfigSyncTunnelTransports : IConfigSync
    {
        public string Name => "TunnelTransports";

        private readonly FileConfig fileConfig;
        public ConfigSyncTunnelTransports(FileConfig fileConfig)
        {
            this.fileConfig = fileConfig;
        }
        public Memory<byte> GetData()
        {
            return MemoryPackSerializer.Serialize(fileConfig.Data.Client.Tunnel.Transports);
        }

        public void SetData(Memory<byte> data)
        {
            fileConfig.Data.Client.Tunnel.Transports = MemoryPackSerializer.Deserialize<List<TunnelTransportItemInfo>>(data.Span);
            fileConfig.Data.Update();
        }
    }
}
