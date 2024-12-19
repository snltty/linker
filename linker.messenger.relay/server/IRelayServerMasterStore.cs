using linker.libs;

namespace linker.messenger.relay.server
{
    public interface IRelayServerMasterStore
    {
        /// <summary>
        /// 主服务器信息
        /// </summary>
        public RelayServerMasterInfo Master { get; }
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
