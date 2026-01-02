
using linker.libs;
namespace linker.messenger.signin
{
    /// <summary>
    /// 登录持久化，不想保存可以不存，实现接口就行
    /// </summary>
    public interface ISignInServerStore : IStore<SignCacheInfo>
    {
        public int CleanDays { get; }
        public bool Enabled { get; }
        public bool Anonymous { get; }
        public string[] Hosts { get; }

        public bool ValidateSuper(string key,string password);
        public void SetSuper(string key, string password);
        public void SetCleanDays(int days);
        public void SetAnonymous(bool anonymous);

        public string[] Exp(string id);
        public void Exp(List<string> ids);
    }
}
