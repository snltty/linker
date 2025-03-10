using linker.libs;
using System.Net;

namespace linker.messenger.relay.server
{
    public interface IRelayServerCdkeyStore
    {
        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public Task<bool> Add(RelayServerCdkeyStoreInfo info);
        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Task<bool> Del(long id);
        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="id"></param>
        /// <param name="userid"></param>
        /// <returns></returns>
        public Task<bool> Del(long id, string userid);

        /// <summary>
        /// 测试卡密是否可用
        /// </summary>
        /// <param name="base64"></param>
        /// <returns></returns>
        public Task<RelayServerCdkeyTestResultInfo> Test(RelayServerCdkeyImportInfo info);
        /// <summary>
        /// 导入卡密
        /// </summary>
        /// <param name="base64"></param>
        /// <returns></returns>
        public Task<string> Import(RelayServerCdkeyImportInfo info);

        /// <summary>
        /// 获取有效的CDKEY
        /// </summary>
        /// <param name="userid"></param>
        /// <returns></returns>
        public Task<List<RelayServerCdkeyStoreInfo>> GetAvailable(string userid);
        /// <summary>
        /// 获取CDKEY列表
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public Task<List<RelayServerCdkeyStoreInfo>> Get(List<long> ids);
        /// <summary>
        /// 消耗流量
        /// </summary>
        /// <param name="dic"></param>
        /// <returns></returns>
        public Task<bool> Traffic(Dictionary<long, long> dic);
        /// <summary>
        /// 获取剩余流量
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public Task<Dictionary<long, long>> GetLastBytes(List<long> ids);
        /// <summary>
        /// 分页
        /// </summary>
        /// <param name="relayServerCdkeyPageRequestInfo"></param>
        /// <returns></returns>
        public Task<RelayServerCdkeyPageResultInfo> Page(RelayServerCdkeyPageRequestInfo relayServerCdkeyPageRequestInfo);
    }

    public sealed class RelayServerCdkeyConfigInfo
    {
        /// <summary>
        /// 加解密密钥
        /// </summary>
#if DEBUG
        public string SecretKey { get; set; } = Helper.GlobalString;
#else
        public string SecretKey { get; set; } = Guid.NewGuid().ToString().ToUpper();
#endif
    }

    public sealed partial class RelayServerCdkeyPageRequestInfo
    {
        public int Page { get; set; }
        public int Size { get; set; }
        public string Order { get; set; }
        public string Sort { get; set; }
        public string UserId { get; set; }
        public string Remark { get; set; }
        public string OrderId { get; set; }
        public string Contact { get; set; }
        public string SecretKey { get; set; }
        public RelayServerCdkeyPageRequestFlag Flag { get; set; }
    }
    [Flags]
    public enum RelayServerCdkeyPageRequestFlag
    {
        All = 0,
        TimeIn = 1,
        TimeOut = 2,
        BytesIn = 4,
        BytesOut = 8,
        UnDeleted = 16,
        Deleted = 32,
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
        public string UserId { get; set; }
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
        /// 流量
        /// </summary>
        public long MaxBytes { get; set; }

        /// <summary>
        /// 成本价
        /// </summary>
        public double CostPrice { get; set; }
        /// <summary>
        /// 原价
        /// </summary>
        public double Price { get; set; }
        /// <summary>
        /// 会员价
        /// </summary>
        public double UserPrice { get; set; }
        /// <summary>
        /// 支付金额
        /// </summary>
        public double PayPrice { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }

        /// <summary>
        /// 订单号
        /// </summary>
        public string OrderId { get; set; }
        /// <summary>
        /// 联系方式
        /// </summary>
        public string Contact { get; set; }
        /// <summary>
        /// 已删除
        /// </summary>
        public bool Deleted { get; set; }
    }

    public sealed partial class RelayServerCdkeyTestResultInfo
    {
        public RelayServerCdkeyOrderInfo Order { get; set; }
        public string Cdkey { get; set; }
        public List<string> Field { get; set; } = new List<string>();
    }
    /// <summary>
    /// 导入中继cdkey
    /// </summary>
    public sealed partial class RelayServerCdkeyImportInfo
    {
        public string SecretKey { get; set; }
        public string UserId { get; set; }
        public string Base64 { get; set; }
    }
    /// <summary>
    /// 导入中继cdkey
    /// </summary>
    public sealed partial class RelayServerCdkeyOrderInfo
    {
        /// <summary>
        /// 总流量
        /// </summary>
        public int GB { get; set; }
        /// <summary>
        /// 带宽
        /// </summary>
        public int Speed { get; set; }
        /// <summary>
        /// 有效年
        /// </summary>
        public string Time { get; set; }

        /// <summary>
        /// 用户标识
        /// </summary>
        public string WidgetUserId { get; set; }
        /// <summary>
        /// 订单号
        /// </summary>
        public string OrderId { get; set; }
        /// <summary>
        /// 联系方式
        /// </summary>
        public string Contact { get; set; }
        /// <summary>
        /// 成本价
        /// </summary>
        public double CostPrice { get; set; }
        /// <summary>
        /// 原价
        /// </summary>
        public double Price { get; set; }
        /// <summary>
        /// 会员价
        /// </summary>
        public double UserPrice { get; set; }
        /// <summary>
        /// 支付金额
        /// </summary>
        public double PayPrice { get; set; }
        /// <summary>
        /// 数量
        /// </summary>
        public int Count { get; set; }

        public string Type { get; set; }
    }
}
