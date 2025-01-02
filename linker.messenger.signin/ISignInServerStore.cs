
using linker.libs;
namespace linker.messenger.signin
{
    /// <summary>
    /// 登录持久化，不想保存可以不存，实现接口就行
    /// </summary>
    public interface ISignInServerStore : IStore<SignCacheInfo>
    {
        /// <summary>
        /// 信标密钥
        /// </summary>
        public string SecretKey { get; }
        /// <summary>
        /// 设置信标密钥
        /// </summary>
        /// <param name="secretKey"></param>
        public void SetSecretKey(string secretKey);
    }
}
