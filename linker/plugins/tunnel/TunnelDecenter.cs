using linker.config;
using linker.libs;
using linker.plugins.decenter;
using MemoryPack;
using System.Collections.Concurrent;
namespace linker.plugins.tunnel
{
    public sealed class TunnelDecenter:IDecenter
    {
        public string Name => "tunnel";
        public VersionManager SyncVersion { get; } = new VersionManager();

        public VersionManager DataVersion { get; } = new VersionManager();
        public ConcurrentDictionary<string, TunnelTransportRouteLevelInfo> Config { get; } = new ConcurrentDictionary<string, TunnelTransportRouteLevelInfo>();

        private readonly TunnelConfigTransfer tunnelConfigTransfer;
        public TunnelDecenter(TunnelConfigTransfer  tunnelConfigTransfer)
        {
            this.tunnelConfigTransfer = tunnelConfigTransfer;
            tunnelConfigTransfer.OnChanged += Refresh;
        }
        public void Refresh()
        {
            SyncVersion.Add();
        }
        public Memory<byte> GetData()
        {
            TunnelTransportRouteLevelInfo tunnelTransportRouteLevelInfo = tunnelConfigTransfer.GetLocalRouteLevel();
            Config.AddOrUpdate(tunnelTransportRouteLevelInfo.MachineId, tunnelTransportRouteLevelInfo, (a, b) => tunnelTransportRouteLevelInfo);
            DataVersion.Add();
            return MemoryPackSerializer.Serialize(tunnelTransportRouteLevelInfo);
        }
        public void SetData(Memory<byte> data)
        {
            TunnelTransportRouteLevelInfo tunnelTransportRouteLevelInfo = MemoryPackSerializer.Deserialize<TunnelTransportRouteLevelInfo>(data.Span);
            Config.AddOrUpdate(tunnelTransportRouteLevelInfo.MachineId, tunnelTransportRouteLevelInfo, (a, b) => tunnelTransportRouteLevelInfo);
            DataVersion.Add();
        }
        public void SetData(List<ReadOnlyMemory<byte>> data)
        {
            List<TunnelTransportRouteLevelInfo> list = data.Select(c => MemoryPackSerializer.Deserialize<TunnelTransportRouteLevelInfo>(c.Span)).ToList();
            foreach (var item in list)
            {
                Config.AddOrUpdate(item.MachineId, item, (a, b) => item);
            }
            TunnelTransportRouteLevelInfo config = tunnelConfigTransfer.GetLocalRouteLevel();
            Config.AddOrUpdate(config.MachineId, config, (a, b) => config);
            DataVersion.Add();
        }
    }
}
