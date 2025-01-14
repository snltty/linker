using linker.libs;

namespace linker.messenger.signin.args
{
    /// <summary>
    /// 版本限制
    /// </summary>
    public sealed class SignInArgsVersionClient : ISignInArgs
    {
        public string Name => "version";
        public async Task<string> Invoke(string host, Dictionary<string, string> args)
        {
            args.TryAdd("version", VersionHelper.version);

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
    /// 版本限制
    /// </summary>
    public sealed class SignInArgsVersionServer : ISignInArgs
    {
        public string Name => "version";
        /// <summary>
        /// 客户端调用
        /// </summary>
        /// <param name="host"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public async Task<string> Invoke(string host, Dictionary<string, string> args)
        {
            await Task.CompletedTask;
            return string.Empty;
        }

        /// <summary>
        /// 服务端调用
        /// </summary>
        /// <param name="signInfo">新登录参数</param>
        /// <param name="cache">之前的登录信息</param>
        /// <returns></returns>
        public async Task<string> Validate(SignInfo signInfo, SignCacheInfo cache)
        {
            if (VersionHelper.Compare(signInfo.Version, "v1.5.0", false) < 0)
            {
                return "need v1.5.0+";
            }

            await Task.CompletedTask;
            return string.Empty;
        }


    }
}
