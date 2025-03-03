using linker.libs;
using linker.messenger.relay.server;
using linker.tunnel.connection;
using System.Net;

namespace linker.messenger.relay.client.transport
{
    /// <summary>
    /// 中继类型
    /// </summary>
    public enum RelayClientType : byte
    {
        Linker = 0,
    }
    /// <summary>
    /// 中继接口
    /// </summary>
    public interface IRelayClientTransport
    {
        /// <summary>
        /// 接口名
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// 中继类型
        /// </summary>
        public RelayClientType Type { get; }
        /// <summary>
        /// 协议
        /// </summary>
        public TunnelProtocolType ProtocolType { get; }
        /// <summary>
        /// 开始中继
        /// </summary>
        /// <param name="relayInfo"></param>
        /// <returns></returns>
        public Task<ITunnelConnection> RelayAsync(RelayInfo relayInfo);
        /// <summary>
        /// 收到别人的中继请求
        /// </summary>
        /// <param name="relayInfo"></param>
        /// <returns></returns>
        public Task<bool> OnBeginAsync(RelayInfo relayInfo, Action<ITunnelConnection> callback);

        /// <summary>
        /// 测试一下中继通不通
        /// </summary>
        /// <param name="relayTestInfo"></param>
        /// <returns></returns>
        public Task<List<RelayServerNodeReportInfo>> RelayTestAsync(RelayTestInfo relayTestInfo);
    }

    /// <summary>
    /// 中继测试
    /// </summary>
    public sealed partial class RelayTestInfo
    {
        public string MachineId { get; set; }
        public string SecretKey { get; set; }
        public IPEndPoint Server { get; set; }
        public string UserId { get; set; }
    }

    /// <summary>
    /// 中继交换数据
    /// </summary>
    public sealed partial class RelayInfo
    {
        /// <summary>
        /// 自己的id
        /// </summary>
        public string FromMachineId { get; set; }
        /// <summary>
        /// 自己的名称
        /// </summary>
        public string FromMachineName { get; set; }
        /// <summary>
        /// 对方id
        /// </summary>
        public string RemoteMachineId { get; set; }
        /// <summary>
        /// 对方名称
        /// </summary>
        public string RemoteMachineName { get; set; }
        /// <summary>
        /// 事务
        /// </summary>
        public string TransactionId { get; set; }
        /// <summary>
        /// 密钥
        /// </summary>
        public string SecretKey { get; set; }
        /// <summary>
        /// 协议名
        /// </summary>
        public string TransportName { get; set; }
        /// <summary>
        /// 流水id
        /// </summary>
        public ulong FlowingId { get; set; }

        /// <summary>
        /// 中继节点id
        /// </summary>
        public string NodeId { get; set; }

        /// <summary>
        /// 服务器，a端选择用什么服务器，就带给b，b直接用，不需要再做复杂的选择
        /// </summary>
        public IPEndPoint Server { get; set; }

        /// <summary>
        /// 是否ssl
        /// </summary>
        public bool SSL { get; set; } = true;

        /// <summary>
        /// UserId
        /// </summary>
        public string UserId { get; set; }
    }


    public sealed partial class RelayServerInfo
    {
        public RelayServerInfo() { }
        /// <summary>
        /// 密钥
        /// </summary>
        public string SecretKey { get; set; } = Helper.GlobalString;
        /// <summary>
        /// 禁用
        /// </summary>
        public bool Disabled { get; set; }
        /// <summary>
        /// 开启ssl
        /// </summary>
        public bool SSL { get; set; } = true;

        public RelayClientType RelayType { get; set; } = RelayClientType.Linker;
    }
}
