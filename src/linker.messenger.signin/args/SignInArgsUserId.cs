namespace linker.messenger.signin.args
{
    /// <summary>
    /// 版本限制
    /// </summary>
    public sealed class SignInArgsUserIdClient : ISignInArgsClient
    {
        public string Name => "userid";
        public SignInArgsLevel Level => SignInArgsLevel.Default;

        private readonly ISignInClientStore signInClientStore;
        public SignInArgsUserIdClient(ISignInClientStore signInClientStore)
        {
            this.signInClientStore = signInClientStore;
        }
        public async Task<string> Invoke(string host, Dictionary<string, string> args)
        {
            args.TryAdd("userid", signInClientStore.Server.UserId);
            return await Task.FromResult(string.Empty);
        }
    }
    public sealed class SignInArgsUserIdServer : ISignInArgsServer
    {
        public string Name => "userid";
        public SignInArgsLevel Level => SignInArgsLevel.Default;

        /// <summary>
        /// 服务端调用
        /// </summary>
        /// <param name="signInfo">新登录参数</param>
        /// <param name="cache">之前的登录信息</param>
        /// <returns></returns>
        public async Task<string> Validate(SignInfo signInfo, SignCacheInfo cache)
        {
            if (signInfo.Args.TryGetValue("userid", out string userid) && string.IsNullOrWhiteSpace(userid) == false)
            {
                signInfo.UserId = userid;
            }
            return await Task.FromResult(string.Empty);
        }
    }
}

namespace linker.messenger.signin
{
    public partial class SignInfo
    {
        public string UserId { get; set; } = string.Empty;
    }

}
