using linker.plugins.signIn.args;
using linker.plugins.signin.messenger;
using linker.config;
using linker.plugins.signIn;

namespace linker.plugins.client
{
    /// <summary>
    /// 给登录加一个唯一ID的参数
    /// </summary>
    public sealed class SignInArgsSecretKeyClient : ISignInArgs
    {
        private readonly FileConfig fileConfig;
        private readonly ClientConfigTransfer clientConfigTransfer;  
        public SignInArgsSecretKeyClient(FileConfig fileConfig, ClientConfigTransfer clientConfigTransfer)
        {
            this.fileConfig = fileConfig;
            this.clientConfigTransfer = clientConfigTransfer;
        }
        public async Task<string> Invoke(string host, Dictionary<string, string> args)
        {
            args.TryAdd("signin-secretkey", clientConfigTransfer.Server.SecretKey);
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
        private readonly FileConfig fileConfig;
        private readonly SignInConfigTransfer signInConfigTransfer;
        public SignInArgsSecretKeyServer(FileConfig fileConfig, SignInConfigTransfer signInConfigTransfer)
        {
            this.fileConfig = fileConfig;
            this.signInConfigTransfer = signInConfigTransfer;
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
            if (string.IsNullOrWhiteSpace(signInConfigTransfer.SecretKey) == false)
            {
                if (signInfo.Args.TryGetValue("signin-secretkey", out string secretkey) == false || secretkey != signInConfigTransfer.SecretKey)
                {
                    return $"server secretkey validate fail";
                }
            }
            await Task.CompletedTask;
            return string.Empty;
        }


    }
}
