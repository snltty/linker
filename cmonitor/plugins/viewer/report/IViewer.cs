using MemoryPack;

namespace cmonitor.plugins.viewer.report
{
    public interface IViewer
    {
        public void Open(bool value, ViewerMode mode);
        public string GetConnectString();
        public void SetConnectString(string connectStr);
    }

    [MemoryPackable]
    public sealed partial class ViewerConfigInfo
    {
        public ViewerMode Mode { get; set; }
        public bool Open { get; set; }
        public string[] Clients { get; set; } = Array.Empty<string>();

        public string ConnectStr { get; set; } = string.Empty;
    }

    public enum ViewerMode : byte
    {
        Client = 0,
        Server = 1,
    }
}
