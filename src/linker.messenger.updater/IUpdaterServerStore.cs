namespace linker.messenger.updater
{
    public interface IUpdaterServerStore
    {
        
        public bool ValidateSecretKey(string key);
        /// <summary>
        /// 设置更新密钥
        /// </summary>
        /// <param name="key"></param>
        public void SetSecretKey(string key);
        public bool Confirm();
    }
}
