
namespace linker.messenger.sync
{
    public interface ISync
    {
        public string Name { get; }
        public Memory<byte> GetData();
        public void SetData(Memory<byte> data);
    }

    public partial class SyncInfo
    {
        public SyncInfo() { }
        public string Name { get; set; }
        public Memory<byte> Data { get; set; }
    }
    public sealed partial class Sync184Info : SyncInfo
    {
        public Sync184Info() { }
        public string[] Ids { get; set; }
    }
}
