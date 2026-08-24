using linker.libs.web;
using System.Collections.Concurrent;

namespace linker.messenger.decenter
{
    /// <summary>
    /// 数据同步
    /// </summary>
    public sealed class DecenterApiController : IApiController
    {
        private readonly CounterDecenter counterDecenter;

        public DecenterApiController(CounterDecenter counterDecenter)
        {
            this.counterDecenter = counterDecenter;
        }

        public void Refresh(ApiControllerParamsInfo param)
        {
            counterDecenter.Refresh();
        }
        /// <summary>
        /// 获取数量
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public CounterInfo GetCounter(ApiControllerParamsInfo param)
        {
            ulong hashCode = ulong.Parse(param.Content);
            if (counterDecenter.DataVersion.Eq(hashCode, out ulong version) == false)
            {
                return new CounterInfo
                {
                    List = counterDecenter.CountDic,
                    HashCode = version
                };
            }
            return new CounterInfo { HashCode = version };
        }

        public sealed class CounterInfo
        {
            public ulong HashCode { get; set; }
            public ConcurrentDictionary<string, ConcurrentDictionary<string, int>> List { get; set; }
        }
    }

}
