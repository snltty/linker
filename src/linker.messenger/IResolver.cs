using System.Net;
using System.Net.Sockets;

namespace linker.messenger
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

        /// <summary>
        /// 穿透节点报告
        /// </summary>
        SForwardReport = 8,

        /// <summary>
        /// 节点联机
        /// </summary>
        NodeConnection = 9,
    }
    public interface IResolver
    {
        public byte Type { get; }
        /// <summary>
        /// TCP
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="memory"></param>
        /// <returns></returns>
        public Task Resolve(Socket socket, Memory<byte> memory);
        /// <summary>
        /// UDP
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="ep"></param>
        /// <param name="memory"></param>
        /// <returns></returns>
        public Task Resolve(Socket socket, IPEndPoint ep, Memory<byte> memory);
    }


}
