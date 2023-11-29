using MemoryPack;

namespace cmonitor.server.client.reports.notify
{
    public interface INotify
    {
        public void Update(NotifyInfo notify);
    }

    [MemoryPackable]
    public sealed partial class NotifyInfo
    {
        public byte Speed { get; set; }
        public string Msg { get; set; }
        public byte Star1 { get; set; } = 1;
        public byte Star2 { get; set; } = 1;
        public byte Star3 { get; set; } = 1;
    }
}
