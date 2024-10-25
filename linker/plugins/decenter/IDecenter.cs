using linker.libs;

namespace linker.plugins.decenter
{
    public interface IDecenter
    {
        public string Name { get; }
        public VersionManager DataVersion { get; }
        public Memory<byte> GetData();
        public void SetData(Memory<byte> data);
        public void SetData(List<ReadOnlyMemory<byte>> data);
    }
}
