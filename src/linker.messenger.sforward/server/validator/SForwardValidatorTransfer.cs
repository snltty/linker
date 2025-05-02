using linker.messenger.signin;
using linker.libs;

namespace linker.messenger.sforward.server.validator
{
    /// <summary>
    /// 验证
    /// </summary>
    public sealed partial class SForwardValidatorTransfer
    {
        private List<ISForwardValidator> validators = new List<ISForwardValidator>();

        public SForwardValidatorTransfer()
        {
        }

        /// <summary>
        /// 加载验证实现类
        /// </summary>
        /// <param name="list"></param>
        public void AddValidators(List<ISForwardValidator> list)
        {
            if (list == null) return;
            validators = validators.Concat(list).Distinct().ToList();
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Info($"load sforward server validator :{string.Join(",", list.Select(c => c.GetType().Name))}");
        }
        /// <summary>
        /// 删除一些验证实现类
        /// </summary>
        /// <param name="names"></param>
        public void RemoveValidators(List<string> names)
        {
            foreach (string name in names)
            {
                ISForwardValidator item = validators.FirstOrDefault(c => c.Name == name);
                if (item != null)
                    validators.Remove(item);
            }
        }

        /// <summary>
        /// 验证
        /// </summary>
        /// <param name="signCacheInfo"></param>
        /// <param name="sForwardAddInfo"></param>
        /// <returns></returns>
        public async Task<string> Validate(SignCacheInfo signCacheInfo, SForwardAddInfo sForwardAddInfo)
        {
            foreach (var item in validators)
            {
                string result = await item.Validate(signCacheInfo, sForwardAddInfo).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(result) == false)
                {
                    return result;
                }
            }
            return string.Empty;
        }

    }
}
