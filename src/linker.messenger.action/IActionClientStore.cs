using System.Collections.Concurrent;

namespace linker.messenger.action
{
    public sealed class ActionInfo
    {
        public string Arg { get; set; } = string.Empty;
        public ConcurrentDictionary<string, string> Args { get; set; } = new ConcurrentDictionary<string, string>();
    }
    public interface IActionClientStore
    {
        /// <summary>
        /// 设置动态验证参数，优先使用
        /// </summary>
        /// <param name="value"></param>
        public void SetActionDynamicArg(string value);
        /// <summary>
        /// 设置静态验证参数，动态参数为空时使用
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void SetActionStaticArg(string key,string value);
        /// <summary>
        /// 获取静态参数
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetActionStaticArg(string key);

        /// <summary>
        /// 网args里添加指定key的值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool TryAddActionArg(string key, Dictionary<string, string> args);
        /// <summary>
        /// 提交更新
        /// </summary>
        /// <returns></returns>
        public bool Confirm();
    }
}
