using linker.plugins.signIn.args;
using linker.plugins.signin.messenger;
using linker.config;

namespace linker.plugins.client
{
    /// <summary>
    /// 添加分组密码
    /// </summary>
    public sealed class SignInArgsGroupPasswordClient : ISignInArgs
    {
        private readonly FileConfig fileConfig;
        private readonly ClientConfigTransfer clientConfigTransfer;
        public SignInArgsGroupPasswordClient(FileConfig fileConfig, ClientConfigTransfer clientConfigTransfer)
        {
            this.fileConfig = fileConfig;
            this.clientConfigTransfer = clientConfigTransfer;
        }
        public async Task<string> Invoke(string host, Dictionary<string, string> args)
        {
            args.TryAdd("signin-gpwd", clientConfigTransfer.Group.Password);
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
    /// 验证分组密码
    /// </summary>
    public sealed class SignInArgsGroupPasswordServer : ISignInArgs
    {
        private readonly FileConfig fileConfig;
        public SignInArgsGroupPasswordServer(FileConfig fileConfig)
        {
            this.fileConfig = fileConfig;
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
            if (signInfo.Args.TryGetValue("signin-gpwd", out string gpwd) && string.IsNullOrWhiteSpace(gpwd) == false)
            {
                signInfo.GroupId = $"{signInfo.GroupId}->{gpwd}";
            }
            await Task.CompletedTask;
            return string.Empty;
        }


    }
}
