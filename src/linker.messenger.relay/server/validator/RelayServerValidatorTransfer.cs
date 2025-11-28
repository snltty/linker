using linker.messenger.signin;
using linker.libs;
using System.Diagnostics.CodeAnalysis;

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
        public void AddValidators(List<IRelayServerValidator> list)
        {
            if (list == null) return;
            validators = validators.Concat(list).Distinct(new RelayServerValidatorEqualityComparer()).ToList();
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Info($"load relay server validator :{string.Join(",", list.Select(c => c.GetType().Name))}");
        }
        /// <summary>
        /// 删除一些验证实现类
        /// </summary>
        /// <param name="names"></param>
        public void RemoveValidators(List<string> names)
        {
            foreach (string name in names)
            {
                IRelayServerValidator item = validators.FirstOrDefault(c => c.Name == name);
                if (item != null)
                    validators.Remove(item);
            }
        }

        /// <summary>
        /// 验证
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="transactionId"></param>
        /// <returns></returns>
        public async Task<string> Validate(SignCacheInfo from, SignCacheInfo to,string transactionId)
        {
            foreach (var item in validators)
            {
                string result = await item.Validate(from, to, transactionId).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(result) == false)
                {
                    return result;
                }
            }
            return string.Empty;
        }
        public async Task<List<RelayServerNodeStoreInfo>> Validate(string userid, SignCacheInfo fromMachine, List<RelayServerNodeStoreInfo> nodes)
        {
            foreach (var item in validators)
            {
                nodes = await item.Validate(userid, fromMachine, nodes).ConfigureAwait(false);
                if (nodes == null || nodes.Count == 0)
                {
                    return [];
                }
            }
            return nodes;
        }

        public sealed class RelayServerValidatorEqualityComparer : IEqualityComparer<IRelayServerValidator>
        {
            public bool Equals(IRelayServerValidator x, IRelayServerValidator y)
            {
                return x.GetType().FullName == y.GetType().FullName;
            }

            public int GetHashCode([DisallowNull] IRelayServerValidator obj)
            {
                return obj.GetHashCode();
            }
        }
    }
}
