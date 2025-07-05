namespace linker.messenger.signin.args
{
    public enum SignInArgsLevel
    {
        Bottom = -999,
        Low = -99,
        Default = 0,
        Hight = 99,
        Top = 999,
    }

    /// <summary>
    /// 登录参数处理
    /// </summary>
    public interface ISignInArgsClient
    {
        public string Name { get; }
        public SignInArgsLevel Level { get; }
        /// <summary>
        /// 添加参数，客户端调用
        /// </summary>
        /// <param name="host"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public Task<string> Invoke(string host, Dictionary<string, string> args);
    }
    public interface ISignInArgsServer
    {
        public string Name { get; }
        public SignInArgsLevel Level { get; }
        /// <summary>
        /// 验证参数，服务端调用
        /// </summary>
        /// <param name="signInfo"></param>
        /// <param name="cache"></param>
        /// <returns></returns>
        public Task<string> Validate(SignInfo signInfo, SignCacheInfo cache);
    }
}
