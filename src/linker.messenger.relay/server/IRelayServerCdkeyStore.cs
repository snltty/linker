namespace linker.messenger.relay.server
{
    public interface IRelayServerCdkeyStore
    {
        public Task<bool> Add(RelayServerCdkeyStoreInfo info);
        public Task<bool> Del(long id);

        /// <summary>
        /// 获取有效的CDKEY
        /// </summary>
        /// <param name="userid"></param>
        /// <returns></returns>
        public Task<List<RelayServerCdkeyStoreInfo>> GetAvailable(string userid);
        public Task<List<RelayServerCdkeyStoreInfo>> Get(List<long> ids);

        public Task<bool> Traffic(Dictionary<long, long> dic);
        public Task<RelayServerCdkeyPageResultInfo> Get(RelayServerCdkeyPageRequestInfo relayServerCdkeyPageRequestInfo);
    }

    public sealed class RelayServerCdkeyConfigInfo
    {
        /// <summary>
        /// 获取可用的CDKEY
        /// </summary>
        public string CdkeyAvailablePostUrl { get; set; } = string.Empty;
        /// <summary>
        /// 分页获取CDKEY
        /// </summary>
        public string CdkeyPagePostUrl { get; set; } = string.Empty;
        /// <summary>
        /// id列表获取CDKEY
        /// </summary>
        public string CdkeyListPostUrl { get; set; } = string.Empty;
        /// <summary>
        /// 报告流量websocket
        /// </summary>
        public string CdkeyTrafficWsUrl { get; set; } = string.Empty;
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
        public List<RelayServerCdkeyStoreInfo> List { get; set; }
    }

    public sealed partial class RelayServerCdkeyAddInfo
    {
        public string SecretKey { get; set; }
        public RelayServerCdkeyStoreInfo Data { get; set; }
    }
    public sealed partial class RelayServerCdkeyDelInfo
    {
        public string SecretKey { get; set; }
        public long CdkeyId { get; set; }
    }

    /// <summary>
    /// 中继CDKEY存储
    /// </summary>
    public sealed partial class RelayServerCdkeyStoreInfo : RelayServerCdkeyInfo
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
        /// 最后使用时间
        /// </summary>
        public DateTime UseTime { get; set; }
        /// <summary>
        /// 允许节点
        /// </summary>
        public List<string> Nodes { get; set; }
        /// <summary>
        /// 流量
        /// </summary>
        public long MaxBytes { get; set; }

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
