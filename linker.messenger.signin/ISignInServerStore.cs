
using linker.libs;
namespace linker.messenger.signin
{
    /// <summary>
    /// 登录持久化，不想保存可以不存，实现接口就行
    /// </summary>
    public interface ISignInServerStore : IStore<SignCacheInfo>
    {
        public string SecretKey { get; }
    }
}
