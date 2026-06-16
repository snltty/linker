
using linker.messenger.mesh;

namespace linker.messenger.store.file
{
    public sealed partial class RunningConfigInfo
    {
        /// <summary>
        /// 打洞历史记录
        /// </summary>
        public MeshHistoryInfo MeshHistory { get; set; } = new MeshHistoryInfo();
    }
}
