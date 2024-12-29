using System.Net;

namespace linker.messenger.tunnel
{
    /// <summary>
    /// 打洞排除IP
    /// </summary>
    public interface ITunnelClientExcludeIP
    {
        public List<IPAddress> Get();
    }
}
