using MemoryPack;

namespace linker.plugins.updater.config
{
    [MemoryPackable]
    public sealed partial class UpdaterConfirmInfo
    {
        public string MachineId { get; set; }
        public string Version { get; set; }
    }
}
