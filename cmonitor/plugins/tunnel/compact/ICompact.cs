using cmonitor.config;
using System.Net;

namespace cmonitor.plugins.tunnel.compact
{
    public interface ICompact
    {
        public string Name { get; }
        public TunnelCompactType Type { get; }
        public Task<TunnelCompactIPEndPoint> GetExternalIPAsync(IPEndPoint server);
    }

    public sealed class TunnelCompactIPEndPoint
    {
        public IPEndPoint Local { get; set; }
        public IPEndPoint Remote { get; set; }
    }
}
