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
        /// 数据版本，从收到数据则更新
        /// </summary>
        public VersionManager DataVersion { get; }
        /// <summary>
        /// 获取本地数据
        /// </summary>
        /// <returns></returns>
        public Memory<byte> GetData();
        /// <summary>
        /// 收到远端数据
        /// </summary>
        /// <param name="data"></param>
        public void SetData(Memory<byte> data);
        /// <summary>
        /// 收到远端数据
        /// </summary>
        /// <param name="data"></param>
        public void SetData(List<ReadOnlyMemory<byte>> data);
    }

    public sealed partial class DecenterSyncInfo
    {
        public DecenterSyncInfo() { }
        public string Name { get; set; }
        public Memory<byte> Data { get; set; }
    }

    public sealed partial class DecenterPullInfo
    {
        public DecenterPullInfo() { }
        public string Name { get; set; }
        public bool Full { get; set; }
    }
}
