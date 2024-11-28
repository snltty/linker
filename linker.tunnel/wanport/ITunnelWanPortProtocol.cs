using linker.libs.extends;
using System.Net;
using System.Text;

namespace linker.tunnel.wanport
{
    /// <summary>
    /// 外网端口协议
    /// </summary>
    public interface ITunnelWanPortProtocol
    {
        public string Name { get; }
        public TunnelWanPortProtocolType ProtocolType { get; }
        /// <summary>
        /// 获取外网端口
        /// </summary>
        /// <param name="inter">本地网卡</param>
        /// <param name="server">服务器</param>
        /// <returns></returns>
        public Task<TunnelWanPortEndPoint> GetAsync(IPAddress inter, IPEndPoint server);

    }

    public sealed class TunnelWanPortEndPoint
    {
        /// <summary>
        /// 内网
        /// </summary>
        public IPEndPoint Local { get; set; }
        /// <summary>
        /// 外网
        /// </summary>
        public IPEndPoint Remote { get; set; }
    }

    [Flags]
    public enum TunnelWanPortProtocolType : byte
    {
        Tcp = 1,
        Udp = 2,
    }
}
