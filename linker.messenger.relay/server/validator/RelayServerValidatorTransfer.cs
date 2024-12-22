using linker.messenger.signin;
using linker.messenger.relay.client.transport;
using linker.libs;

namespace linker.messenger.relay.server.validator
{
    /// <summary>
    /// 中继验证
    /// </summary>
    public sealed partial class RelayServerValidatorTransfer
    {
        private List<IRelayServerValidator> validators = new List<IRelayServerValidator>();

        public RelayServerValidatorTransfer()
        {
        }

        /// <summary>
        /// 加载中继验证实现类
        /// </summary>
        /// <param name="list"></param>
        public void LoadValidators(List<IRelayServerValidator> list)
        {
            validators = list;
            LoggerHelper.Instance.Info($"load relay server validator :{string.Join(",", validators.Select(c => c.GetType().Name))}");
        }

        /// <summary>
        /// 验证
        /// </summary>
        /// <param name="relayInfo"></param>
        /// <param name="cache"></param>
        /// <param name="cache1"></param>
        /// <returns></returns>
        public async Task<string> Validate(RelayInfo relayInfo, SignCacheInfo cache, SignCacheInfo cache1)
        {
            foreach (var item in validators)
            {
                string result = await item.Validate(relayInfo, cache, cache1);
                if (string.IsNullOrWhiteSpace(result) == false)
                {
                    return result;
                }
            }
            return string.Empty;
        }
    }
}
