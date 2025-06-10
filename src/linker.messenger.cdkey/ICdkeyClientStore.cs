namespace linker.messenger.cdkey
{
    public interface ICdkeyClientStore
    {
        public string SecretKey { get; }

        /// <summary>
        /// 设置密钥
        /// </summary>
        /// <param name="secretKey"></param>
        /// <returns></returns>
        public bool SetSecretKey(string secretKey);
    }

}
