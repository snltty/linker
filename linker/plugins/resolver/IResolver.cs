using System.Net;
using System.Net.Sockets;

namespace linker.plugins.resolver
{
    public enum ResolverType : byte
    {
        /// <summary>
        /// 外网端口
        /// </summary>
        External = 0,
        /// <summary>
        /// 信标
        /// </summary>
        Messenger = 1,
        /// <summary>
        /// 中继
        /// </summary>
        Relay = 2,
        /// <summary>
        /// socks4
        /// </summary>
        Socks4 = 4,
        /// <summary>
        /// socks5
        /// </summary>
        Socks5 = 5,
        /// <summary>
        /// 流量统计报告
        /// </summary>
        FlowReport = 6,
        /// <summary>
        /// 中继节点报告
        /// </summary>
        RelayReport = 7,
    }
    public interface IResolver
    {
        public ResolverType Type { get; }
        public Task Resolve(Socket socket, Memory<byte> memory);
        public Task Resolve(Socket socket, IPEndPoint ep, Memory<byte> memory);
    }


}
