using linker.tunnel.connection;

namespace linker.messenger.pcp
{
    public sealed class PcpHistoryInfo
    {
        public PcpHistoryInfo() { }

        public List<string> History { get; set; } = new List<string>();
    }

    public interface IPcpStore
    {
        public PcpHistoryInfo PcpHistory { get; }
        public void AddHistory(ITunnelConnection connection);
    }
}
