using System.Net;

namespace cmonitor.plugins.tunnel.compact
{
    public interface ICompact
    {
        public string Name { get; }
        public Task<TunnelCompactIPEndPoint> GetTcpExternalIPAsync(IPEndPoint server);
        public Task<TunnelCompactIPEndPoint> GetUdpExternalIPAsync(IPEndPoint server);
    }

    public sealed class TunnelCompactIPEndPoint
    {
        public IPEndPoint Local { get; set; }
        public IPEndPoint Remote { get; set; }
    }
}
