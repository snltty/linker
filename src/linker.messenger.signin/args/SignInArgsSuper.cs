namespace linker.messenger.signin.args
{
    public sealed class SignInArgsSuperClient : ISignInArgsClient
    {
        public string Name => "super";

        public SignInArgsLevel Level => SignInArgsLevel.Default;

        private readonly ISignInClientStore signInClientStore;
        public SignInArgsSuperClient(ISignInClientStore signInClientStore)
        {
            this.signInClientStore = signInClientStore;
        }
        public async Task<string> Invoke(string host, Dictionary<string, string> args)
        {
            args.TryAdd("signin-key", signInClientStore.Server.SuperKey);
            args.TryAdd("signin-password", signInClientStore.Server.SuperPassword);
            return await Task.FromResult(string.Empty);
        }
    }

    public sealed class SignInArgsSuperServer : ISignInArgsServer
    {
        public string Name => "super";
        public SignInArgsLevel Level => SignInArgsLevel.Default;

        private readonly ISignInServerStore signInServerStore;
        public SignInArgsSuperServer(ISignInServerStore signInServerStore)
        {
            this.signInServerStore = signInServerStore;
        }
        /// <summary>
        /// 验证参数
        /// </summary>
        /// <param name="signInfo">新登录参数</param>
        /// <param name="cache">之前的登录信息</param>
        /// <returns></returns>
        public async Task<string> Validate(SignInfo signInfo, SignCacheInfo cache)
        {
            if(signInServerStore.Enabled == false)
            {
                return $"sign in disabled";
            }

            if(signInfo.Args.TryGetValue("signin-key", out string key) && signInfo.Args.TryGetValue("signin-password", out string password))
            {
                signInfo.Super = signInServerStore.ValidateSuper(key, password);
            }
            else if(signInfo.Args.TryGetValue("signin-secretkey", out string secretkey))
            {
                signInfo.Super = signInServerStore.ValidateSuper(secretkey, secretkey);
            }

            if (signInfo.Super == false && signInServerStore.Anonymous == false)
            {
                return $"need sign key and password";
            }
            return await Task.FromResult(string.Empty);
        }


    }
}


namespace linker.messenger.signin
{
    public partial class SignInfo
    {
        public bool Super { get; set; } = false;
    }

}