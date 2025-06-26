namespace linker.messenger.signin.args
{
    /// <summary>
    /// 给登录加一个唯一ID的参数
    /// </summary>
    public sealed class SignInArgsSecretKeyClient : ISignInArgsClient
    {
        public string Name => "secretKey";

        public SignInArgsLevel Level => SignInArgsLevel.Default;

        private readonly ISignInClientStore signInClientStore;
        public SignInArgsSecretKeyClient(ISignInClientStore signInClientStore)
        {
            this.signInClientStore = signInClientStore;
        }
        public async Task<string> Invoke(string host, Dictionary<string, string> args)
        {
            args.TryAdd("signin-secretkey", signInClientStore.Server.SecretKey);
            await Task.CompletedTask.ConfigureAwait(false);
            return string.Empty;
        }
    }

    /// <summary>
    /// 验证登录唯一参数
    /// </summary>
    public sealed class SignInArgsSecretKeyServer : ISignInArgsServer
    {
        public string Name => "secretKey";
        public SignInArgsLevel Level => SignInArgsLevel.Default;

        private readonly ISignInServerStore signInServerStore;
        public SignInArgsSecretKeyServer(ISignInServerStore signInServerStore)
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
            signInfo.Args.TryGetValue("signin-secretkey", out string secretkey);
            if (signInServerStore.ValidateSecretKey(secretkey) == false)
            {
                return $"server secretkey validate fail";
            }
            await Task.CompletedTask.ConfigureAwait(false);
            return string.Empty;
        }


    }
}
