using linker.messenger.signin;
using linker.libs;

namespace linker.messenger.reverse.server.validator
{
    /// <summary>
    /// 验证
    /// </summary>
    public sealed partial class ReverseValidatorTransfer
    {
        private List<IReverseValidator> validators = new List<IReverseValidator>();

        public ReverseValidatorTransfer()
        {
        }

        /// <summary>
        /// 加载验证实现类
        /// </summary>
        /// <param name="list"></param>
        public void AddValidators(List<IReverseValidator> list)
        {
            if (list == null) return;
            validators = validators.Concat(list).Distinct().ToList();
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Info($"load Reverse server validator :{string.Join(",", list.Select(c => c.GetType().Name))}");
        }
        /// <summary>
        /// 删除一些验证实现类
        /// </summary>
        /// <param name="names"></param>
        public void RemoveValidators(List<string> names)
        {
            foreach (string name in names)
            {
                IReverseValidator item = validators.FirstOrDefault(c => c.Name == name);
                if (item != null)
                    validators.Remove(item);
            }
        }

        /// <summary>
        /// 验证
        /// </summary>
        /// <param name="signCacheInfo"></param>
        /// <param name="ReverseAddInfo"></param>
        /// <returns></returns>
        public async Task<string> Validate(SignCacheInfo signCacheInfo, ReverseAddInfo ReverseAddInfo)
        {
            foreach (var item in validators)
            {
                string result = await item.Validate(signCacheInfo, ReverseAddInfo).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(result) == false)
                {
                    return result;
                }
            }
            return string.Empty;
        }

    }
}
