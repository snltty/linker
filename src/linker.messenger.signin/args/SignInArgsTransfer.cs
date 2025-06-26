namespace linker.messenger.signin.args
{
    /// <summary>
    /// 登录参数处理
    /// </summary>
    public sealed partial class SignInArgsTransfer
    {
        private List<ISignInArgsClient> clients = new List<ISignInArgsClient>();
        private List<ISignInArgsServer> servers = new List<ISignInArgsServer>();

        public SignInArgsTransfer()
        {
        }

        /// <summary>
        /// 加载所有登录参数实现类
        /// </summary>
        /// <param name="list"></param>
        public void AddArgs(List<ISignInArgsClient> list)
        {
            clients = clients.Concat(list).Distinct().OrderByDescending(c=>c.Level).ToList();
        }

        /// <summary>
        /// 加载所有登录参数实现类
        /// </summary>
        /// <param name="list"></param>
        public void AddArgs(List<ISignInArgsServer> list)
        {
            servers = servers.Concat(list).Distinct().OrderByDescending(c => c.Level).ToList();
        }

        /// <summary>
        /// 客户端调用
        /// </summary>
        /// <param name="host"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public async Task<string> Invoke(string host, Dictionary<string, string> args)
        {
            foreach (var item in clients)
            {
                string result = await item.Invoke(host, args).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(result) == false)
                {
                    return result;
                }
            }
            return string.Empty;
        }
        /// <summary>
        /// 服务器调用
        /// </summary>
        /// <param name="signInfo"></param>
        /// <param name="cache"></param>
        /// <returns></returns>
        public async Task<string> Validate(SignInfo signInfo, SignCacheInfo cache)
        {
            foreach (var item in servers)
            {
                string result = await item.Validate(signInfo, cache).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(result) == false)
                {
                    return result;
                }
            }
            return string.Empty;
        }
    }
}
