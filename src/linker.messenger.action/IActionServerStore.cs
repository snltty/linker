namespace linker.messenger.action
{
    public interface IActionServerStore
    {
        /// <summary>
        /// 登录验证地址
        /// </summary>
        public string SignInActionUrl{ get; }
        /// <summary>
        /// 中继验证地址
        /// </summary>
        public string RelayActionUrl { get; }
        /// <summary>
        /// 中继节点验证地址
        /// </summary>
        public string RelayNodeUrl { get; }
        /// <summary>
        /// 内网穿透验证地址
        /// </summary>
        public string SForwardActionUrl { get; }

        /// <summary>
        /// 登录验证地址
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public bool SetSignInActionUrl(string url);
        /// <summary>
        /// 中继验证地址
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public bool SetRelayActionUrl(string url);
        public bool SetRelayNodeUrl(string url);
        /// <summary>
        /// 登录验证地址
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public bool SetSForwardActionUrl(string url);

        /// <summary>
        /// 从args里获取验证参数
        /// </summary>
        /// <param name="args"></param>
        /// <param name="str"></param>
        /// <param name="machineKey"></param>
        /// <returns></returns>
        public bool TryGetActionArg(Dictionary<string, string> args, out string str, out string machineKey);

        /// <summary>
        /// 提交更新
        /// </summary>
        /// <returns></returns>
        public bool Confirm();
    }
}
