using linker.tunnel.connection;

namespace linker.messenger.mesh
{
    public sealed class MeshHistoryInfo
    {
        public MeshHistoryInfo() { }

        public List<string> History { get; set; } = new List<string>();
    }

    public interface IMeshStore
    {
        public MeshHistoryInfo MeshHistory { get; }
        public void AddHistory(ITunnelConnection connection);
        public void RemoveHistorys(List<string> historys);
    }
}
