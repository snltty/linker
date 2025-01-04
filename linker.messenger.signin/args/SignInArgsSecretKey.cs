namespace linker.messenger.signin.args
{
    /// <summary>
    /// 给登录加一个唯一ID的参数
    /// </summary>
    public sealed class SignInArgsSecretKeyClient : ISignInArgs
    {
        public string Name => "secretKey";

        private readonly ISignInClientStore signInClientStore;
        public SignInArgsSecretKeyClient(ISignInClientStore signInClientStore)
        {
            this.signInClientStore = signInClientStore;
        }
        public async Task<string> Invoke(string host, Dictionary<string, string> args)
        {
            args.TryAdd("signin-secretkey", signInClientStore.Server.SecretKey);
            await Task.CompletedTask;
            return string.Empty;
        }

        public async Task<string> Validate(SignInfo signInfo, SignCacheInfo cache)
        {
            await Task.CompletedTask;
            return string.Empty;
        }
    }

    /// <summary>
    /// 验证登录唯一参数
    /// </summary>
    public sealed class SignInArgsSecretKeyServer : ISignInArgs
    {
        public string Name => "secretKey";
        private readonly ISignInServerStore signInServerStore;
        public SignInArgsSecretKeyServer(ISignInServerStore signInServerStore)
        {
            this.signInServerStore = signInServerStore;
        }
        public async Task<string> Invoke(string host, Dictionary<string, string> args)
        {
            await Task.CompletedTask;
            return string.Empty;
        }

        /// <summary>
        /// 验证参数
        /// </summary>
        /// <param name="signInfo">新登录参数</param>
        /// <param name="cache">之前的登录信息</param>
        /// <returns></returns>
        public async Task<string> Validate(SignInfo signInfo, SignCacheInfo cache)
        {
            if (string.IsNullOrWhiteSpace(signInServerStore.SecretKey) == false)
            {
                if (signInfo.Args.TryGetValue("signin-secretkey", out string secretkey) == false || secretkey != signInServerStore.SecretKey)
                {
                    return $"server secretkey validate fail";
                }
            }
            await Task.CompletedTask;
            return string.Empty;
        }


    }
}
