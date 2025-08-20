using linker.libs;

namespace linker.messenger.sforward.server
{
    /// <summary>
    /// 穿透主机存储器
    /// </summary>
    public interface ISForwardServerMasterStore
    {
        /// <summary>
        /// 主服务器信息
        /// </summary>
        public SForwardServerMasterInfo Master { get; }

        /// <summary>
        /// 设置
        /// </summary>
        /// <param name="info"></param>
        public void SetInfo(SForwardServerMasterInfo info);

        /// <summary>
        /// 提交
        /// </summary>
        /// <returns></returns>
        public bool Confirm();
    }

    public sealed class SForwardServerMasterInfo
    {
#if DEBUG
        public string SecretKey { get; set; } = Helper.GlobalString;
#else
        public string SecretKey { get; set; } = Guid.NewGuid().ToString().ToUpper();
#endif
    }

}
