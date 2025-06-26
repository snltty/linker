using linker.libs;

namespace linker.messenger.signin.args
{
    /// <summary>
    /// 版本限制
    /// </summary>
    public sealed class SignInArgsVersionClient : ISignInArgsClient
    {
        public string Name => "version";
        public SignInArgsLevel Level =>  SignInArgsLevel.Default;

        public async Task<string> Invoke(string host, Dictionary<string, string> args)
        {
            args.TryAdd("version", VersionHelper.Version);
            return await Task.FromResult(string.Empty);
        }
    }

    /// <summary>
    /// 版本限制
    /// </summary>
    public sealed class SignInArgsVersionServer : ISignInArgsServer
    {
        public string Name => "version";
        public SignInArgsLevel Level => SignInArgsLevel.Default;

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
            return await Task.FromResult(string.Empty);
        }
    }
}
