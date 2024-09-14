using linker.plugins.signIn.args;
using linker.plugins.signin.messenger;
using linker.config;

namespace linker.plugins.client
{
    /// <summary>
    /// 给登录加一个唯一ID的参数
    /// </summary>
    public sealed class SignInArgsSecretKeyClient : ISignInArgs
    {
        private readonly FileConfig fileConfig;
        public SignInArgsSecretKeyClient(FileConfig fileConfig)
        {
            this.fileConfig = fileConfig;
        }
        public async Task<string> Invoke(Dictionary<string, string> args)
        {
            args.TryAdd("signin-secretkey", fileConfig.Data.Client.ServerSecretKey);

            await Task.CompletedTask;
            return string.Empty;
        }

        public async Task<string> Verify(SignInfo signInfo, SignCacheInfo cache)
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
        public SignInArgsSecretKeyServer(FileConfig fileConfig)
        {
            this.fileConfig = fileConfig;
        }
        public async Task<string> Invoke(Dictionary<string, string> args)
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
        public async Task<string> Verify(SignInfo signInfo, SignCacheInfo cache)
        {
            if(string.IsNullOrWhiteSpace(fileConfig.Data.Server.SignIn.SecretKey) == false)
            {
                if (signInfo.Args.TryGetValue("signin-secretkey", out string secretkey) == false || secretkey != fileConfig.Data.Server.SignIn.SecretKey)
                {
                    return $"server secretkey validate fail";
                }
            }
            await Task.CompletedTask;
            return string.Empty;
        }


    }
}
