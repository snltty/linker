using System.Net;

namespace cmonitor.plugins.tunnel.compact
{
    public interface ICompact
    {
        public string Type { get; }
        public Task<CompactIPEndPoint> GetTcpExternalIPAsync(IPEndPoint server);
        public Task<CompactIPEndPoint> GetUdpExternalIPAsync(IPEndPoint server);
    }

    public sealed class CompactIPEndPoint
    {
        public IPEndPoint Local { get; set; }
        public IPEndPoint Remote { get; set; }
    }
}
