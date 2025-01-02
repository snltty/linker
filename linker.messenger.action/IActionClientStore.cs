namespace linker.messenger.action
{
    public sealed class ActionInfo
    {
        public string Arg { get; set; } = string.Empty;
        public Dictionary<string, string> Args { get; set; } = new Dictionary<string, string>();
    }
    public interface IActionClientStore
    {
        /// <summary>
        /// 设置动态验证参数，优先使用
        /// </summary>
        /// <param name="action"></param>
        public void SetActionArg(string action);
        /// <summary>
        /// 设置静态验证参数，动态参数为空时使用
        /// </summary>
        /// <param name="actions">action参数列表，host->arg，不同的服务器不同的参数</param>
        public void SetActionArgs(Dictionary<string, string> actions);
        /// <summary>
        /// 从配置里获取验证参数，添加到args
        /// </summary>
        /// <param name="host">当前服务器地址</param>
        /// <param name="args">一个字典</param>
        /// <returns></returns>
        public bool TryAddActionArg(string host, Dictionary<string, string> args);

        /// <summary>
        /// 提交更新
        /// </summary>
        /// <returns></returns>
        public bool Confirm();
    }
}
