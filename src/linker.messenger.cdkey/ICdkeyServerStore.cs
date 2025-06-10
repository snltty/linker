using linker.libs;

namespace linker.messenger.cdkey
{
    public interface ICdkeyServerStore
    {
        /// <summary>
        /// 验证密钥
        /// </summary>
        /// <param name="secretKey"></param>
        /// <returns></returns>
        public bool ValidateSecretKey(string secretKey);
        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public Task<bool> Add(CdkeyStoreInfo info);
        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Task<bool> Del(int id);
        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="id"></param>
        /// <param name="userid"></param>
        /// <returns></returns>
        public Task<bool> Del(int id, string userid);

        /// <summary>
        /// 测试卡密是否可用
        /// </summary>
        /// <param name="base64"></param>
        /// <returns></returns>
        public Task<CdkeyTestResultInfo> Test(CdkeyImportInfo info);
        /// <summary>
        /// 导入卡密
        /// </summary>
        /// <param name="base64"></param>
        /// <returns></returns>
        public Task<string> Import(CdkeyImportInfo info);

        /// <summary>
        /// 获取有效的CDKEY
        /// </summary>
        /// <param name="userid"></param>
        /// <returns></returns>
        public Task<List<CdkeyStoreInfo>> GetAvailable(string userid, string type);
        /// <summary>
        /// 获取CDKEY列表
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public Task<List<CdkeyStoreInfo>> Get(List<int> ids);
        /// <summary>
        /// 消耗流量
        /// </summary>
        /// <param name="dic"></param>
        /// <returns></returns>
        public Task<bool> Traffic(Dictionary<int, long> dic);
        /// <summary>
        /// 获取剩余流量
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public Task<Dictionary<int, long>> GetLastBytes(List<int> ids);
        /// <summary>
        /// 分页
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public Task<CdkeyPageResultInfo> Page(CdkeyPageRequestInfo info);
    }

    public sealed class CdkeyConfigInfo
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

    /// <summary>
    /// 搜索CDKEY分页请求信息
    /// </summary>
    public sealed partial class CdkeyPageRequestInfo
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
        public string Type { get; set; }
        public CdkeyPageRequestFlag Flag { get; set; }
    }
    [Flags]
    public enum CdkeyPageRequestFlag
    {
        All = 0,
        TimeIn = 1,
        TimeOut = 2,
        BytesIn = 4,
        BytesOut = 8,
        UnDeleted = 16,
        Deleted = 32,
    }

    /// <summary>
    /// 搜索结果
    /// </summary>
    public sealed partial class CdkeyPageResultInfo
    {
        public int Page { get; set; }
        public int Size { get; set; }
        public int Count { get; set; }
        public List<CdkeyStoreInfo> List { get; set; }
    }

    /// <summary>
    /// 添加cdkey
    /// </summary>
    public sealed partial class CdkeyAddInfo
    {
        public string SecretKey { get; set; }
        public CdkeyStoreInfo Data { get; set; }
    }
    /// <summary>
    /// 删除cdkey
    /// </summary>
    public sealed partial class CdkeyDelInfo
    {
        public string SecretKey { get; set; }
        public string UserId { get; set; }
        public int Id { get; set; }
    }

    /// <summary>
    /// cdkey
    /// </summary>
    public partial class CdkeyInfo
    {
        public int Id { get; set; }
        /// <summary>
        /// 带宽Mbps
        /// </summary>
        public double Bandwidth { get; set; }
        /// <summary>
        /// 剩余流量
        /// </summary>
        public long LastBytes { get; set; }
    }
    /// <summary>
    /// CDKEY存储
    /// </summary>
    public sealed partial class CdkeyStoreInfo : CdkeyInfo
    {
        /// <summary>
        /// 类别
        /// </summary>
        public string Type { get; set; }
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

    /// <summary>
    /// cdkey测试结果
    /// </summary>
    public sealed partial class CdkeyTestResultInfo
    {
        public CdkeyOrderInfo Order { get; set; }
        public string Cdkey { get; set; }
        public List<string> Field { get; set; } = new List<string>();
    }
    /// <summary>
    /// 导入cdkey
    /// </summary>
    public sealed partial class CdkeyImportInfo
    {
        public string SecretKey { get; set; }
        public string UserId { get; set; }
        public string Base64 { get; set; }
    }
    /// <summary>
    /// cdkey订单
    /// </summary>
    public sealed partial class CdkeyOrderInfo
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
