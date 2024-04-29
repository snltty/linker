using MemoryPack;

namespace cmonitor.plugins.notify.report
{
    public interface INotify
    {
        public void Update(NotifyInfo notify);
    }

    [MemoryPackable]
    public sealed partial class NotifyInfo
    {
        public string GroupId { get; set;}
        public byte Speed { get; set; }
        public string Msg { get; set; }
        public byte Star1 { get; set; } = 1;
        public byte Star2 { get; set; } = 1;
        public byte Star3 { get; set; } = 1;
    }
}
