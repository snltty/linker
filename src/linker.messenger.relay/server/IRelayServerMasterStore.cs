using linker.libs;

namespace linker.messenger.relay.server
{
    /// <summary>
    /// 中继主机存储器
    /// </summary>
    public interface IRelayServerMasterStore
    {
        /// <summary>
        /// 主服务器信息
        /// </summary>
        public RelayServerMasterInfo Master { get; }

        /// <summary>
        /// 设置
        /// </summary>
        /// <param name="info"></param>
        public void SetInfo(RelayServerMasterInfo info);

        /// <summary>
        /// 提交
        /// </summary>
        /// <returns></returns>
        public bool Confirm();
    }

    public sealed class RelayServerMasterInfo
    {
#if DEBUG
        public string SecretKey { get; set; } = Helper.GlobalString;
#else
        public string SecretKey { get; set; } = Guid.NewGuid().ToString().ToUpper();
#endif
    }

}
