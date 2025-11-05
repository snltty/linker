using linker.libs;

namespace linker.messenger.decenter
{
    public interface IDecenter
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// 同步版本，版本变化则同步
        /// </summary>
        public VersionManager PushVersion { get; }
        /// <summary>
        /// 数据版本，收到新数据则版本变化
        /// </summary>
        public VersionManager DataVersion { get; }

        public bool Force { get; }

        /// <summary>
        /// 获取本地数据
        /// </summary>
        /// <returns></returns>
        public Memory<byte> GetData();
        /// <summary>
        /// 收到远端数据
        /// </summary>
        /// <param name="data"></param>
        public void AddData(Memory<byte> data);
        /// <summary>
        /// 收到远端数据
        /// </summary>
        /// <param name="data"></param>
        public void AddData(List<ReadOnlyMemory<byte>> data);

        /// <summary>
        /// 需要清理数据
        /// </summary>
        public void ClearData();
        /// <summary>
        /// 需要处理数据
        /// </summary>
        public void ProcData();
    }

    public sealed partial class DecenterSyncInfo
    {
        public DecenterSyncInfo() { }
        public string Name { get; set; }
        public Memory<byte> Data { get; set; }
    }

    public sealed partial class DecenterPullPageInfo
    {
        public DecenterPullPageInfo() { }
        public string Name { get; set; }
        public int Page { get; set; }
        public int Size { get; set; }
    }
    public sealed partial class DecenterPullPageResultInfo
    {
        public DecenterPullPageResultInfo() { }
        public int Page { get; set; }
        public int Size { get; set; }
        public int Count { get; set; }
        public List<Memory<byte>> List { get; set; }
    }
}
