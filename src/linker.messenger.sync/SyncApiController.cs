using linker.libs.extends;
using linker.libs.web;
using linker.messenger.api;
namespace linker.messenger.sync
{
    /// <summary>
    /// 数据同步控制器
    /// </summary>
    public sealed class SyncApiController : IApiController
    {
        private readonly SyncTreansfer syncTreansfer;

        public SyncApiController(SyncTreansfer syncTreansfer)
        {
            this.syncTreansfer = syncTreansfer;
        }
        
        /// <summary>
        /// 获取所有名称
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public List<string> Names(ApiControllerParamsInfo param)
        {
            return syncTreansfer.GetNames();
        }

        /// <summary>
        /// 同步数据
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        [Access(AccessValue.Sync)]
        public async Task<bool> Sync(ApiControllerParamsInfo param)
        {
            SyncInfo info = param.Content.DeJson<SyncInfo>();
            await syncTreansfer.Sync(info.Names, info.Ids).ConfigureAwait(false);
            return true;
        }

        public sealed class SyncInfo
        {
            public string[] Names { get; set; } = [];
            public string[] Ids { get; set; } = [];
        }
    }

}
