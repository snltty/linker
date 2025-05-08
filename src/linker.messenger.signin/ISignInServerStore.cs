
using linker.libs;
namespace linker.messenger.signin
{
    /// <summary>
    /// 登录持久化，不想保存可以不存，实现接口就行
    /// </summary>
    public interface ISignInServerStore : IStore<SignCacheInfo>
    {
        public int CleanDays { get; }

        public bool ValidateSecretKey(string key);
        /// <summary>
        /// 设置信标密钥
        /// </summary>
        /// <param name="secretKey"></param>
        public void SetSecretKey(string secretKey);
        public void SetCleanDays(int days);

        public bool Exp(string id);
    }
}
