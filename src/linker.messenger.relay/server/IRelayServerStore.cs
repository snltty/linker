namespace linker.messenger.relay.server
{
    public interface IRelayServerStore
    {
        /// <summary>
        /// 提交
        /// </summary>
        /// <returns></returns>
        public bool Confirm();
    }
}
