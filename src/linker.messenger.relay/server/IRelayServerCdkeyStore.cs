namespace linker.messenger.relay.server
{
    public interface IRelayServerCdkeyStore
    {
        public Task<bool> Add(RelayServerCdkeyInfo info);
        public Task<bool> Del(string id);

        /// <summary>
        /// 获取有效的CDKEY
        /// </summary>
        /// <param name="userid"></param>
        /// <returns></returns>
        public Task<List<RelayServerCdkeyInfo>> Get(string userid);
        public Task<RelayServerCdkeyPageResultInfo> Get(RelayServerCdkeyPageRequestInfo relayServerCdkeyPageRequestInfo);
    }

    public sealed partial class RelayServerCdkeyPageRequestInfo
    {
        public int Page { get; set; }
        public int Size { get; set; }
        public string Order { get; set; }
        public string Sort { get; set; }
        public string UserId { get; set; }
        public string Remark { get; set; }
        public string SecretKey { get; set; }
    }
    public sealed partial class RelayServerCdkeyPageResultInfo
    {
        public int Page { get; set; }
        public int Size { get; set; }
        public int Count { get; set; }
        public List<RelayServerCdkeyInfo> List { get; set; }
    }

    public sealed partial class RelayServerCdkeyAddInfo
    {
        public string SecretKey { get; set; }
        public RelayServerCdkeyInfo Data { get; set; }
    }
    public sealed partial class RelayServerCdkeyDelInfo
    {
        public string SecretKey { get; set; }
        public string Id { get; set; }
    }

    /// <summary>
    /// 中继CDKEY
    /// </summary>
    public sealed partial class RelayServerCdkeyInfo
    {
        public string Id { get; set; }

        /// <summary>
        /// 用户标识
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// KEY
        /// </summary>
        public string CdKey { get; set; }
        /// <summary>
        /// 添加时间
        /// </summary>
        public DateTime AddTime { get; set; }
        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime StartTime { get; set; }
        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime EndTime { get; set; }
        /// <summary>
        /// 允许节点
        /// </summary>
        public List<string> Nodes { get; set; }
        /// <summary>
        /// 带宽Mbps
        /// </summary>
        public double Bandwidth { get; set; }
        /// <summary>
        /// 流量
        /// </summary>
        public ulong MaxBytes { get; set; }
        /// <summary>
        /// 剩余流量
        /// </summary>
        public ulong LastBytes { get; set; }

        /// <summary>
        /// 原价
        /// </summary>
        public double Memory { get; set; }
        /// <summary>
        /// 支付金额
        /// </summary>
        public double PayMemory { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }
    }
}
