namespace linker.messenger.signin.args
{
    /// <summary>
    /// 登录参数处理
    /// </summary>
    public interface ISignInArgs
    {
        public string Name { get; }
        /// <summary>
        /// 添加参数，客户端调用
        /// </summary>
        /// <param name="host"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public Task<string> Invoke(string host, Dictionary<string, string> args);
        /// <summary>
        /// 验证参数，服务端调用
        /// </summary>
        /// <param name="signInfo"></param>
        /// <param name="cache"></param>
        /// <returns></returns>
        public Task<string> Validate(SignInfo signInfo, SignCacheInfo cache);
    }

}
