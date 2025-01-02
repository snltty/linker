namespace linker.messenger.updater
{
    public interface IUpdaterServerStore
    {
        /// <summary>
        /// 更新密钥
        /// </summary>
        public string SecretKey { get; }
        /// <summary>
        /// 设置更新密钥
        /// </summary>
        /// <param name="key"></param>
        public void SetSecretKey(string key);
        public bool Confirm();
    }
}
