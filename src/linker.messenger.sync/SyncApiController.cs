using linker.libs.extends;
using linker.libs.web;
using linker.messenger.api;
namespace linker.messenger.sync
{
    public sealed class SyncApiController : IApiController
    {
        private readonly SyncTreansfer syncTreansfer;

        public SyncApiController(SyncTreansfer syncTreansfer)
        {
            this.syncTreansfer = syncTreansfer;
        }

        public List<string> Names(ApiControllerParamsInfo param)
        {
            return syncTreansfer.GetNames();
        }

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
