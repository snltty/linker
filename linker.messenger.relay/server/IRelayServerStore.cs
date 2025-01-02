namespace linker.messenger.relay.server
{
    public interface IRelayServerStore
    {
        /// <summary>
        /// 中继密钥
        /// </summary>
        public string SecretKey { get; }

        /// <summary>
        /// 设置中继密钥
        /// </summary>
        /// <param name="secretKey"></param>
        public void SetSecretKey(string secretKey);

        /// <summary>
        /// 提交
        /// </summary>
        /// <returns></returns>
        public bool Confirm();
    }
}
