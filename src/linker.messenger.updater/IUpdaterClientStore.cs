namespace linker.messenger.updater
{
    public interface IUpdaterClientStore
    {
        public UpdaterConfigClientInfo Info { get; }
        /// <summary>
        /// 设置更新密钥
        /// </summary>
        /// <param name="key"></param>
        public void SetSecretKey(string key);
        /// <summary>
        /// 设置同步状态
        /// </summary>
        /// <param name="value"></param>
        public void SetSync2Server(bool value);
    }
}
