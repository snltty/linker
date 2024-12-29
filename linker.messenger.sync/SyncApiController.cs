using linker.libs.api;
using linker.libs.extends;
namespace linker.messenger.sync
{
    public sealed class SyncApiController : IApiController
    {
        private readonly SyncTreansfer syncTreansfer;

        public SyncApiController(SyncTreansfer syncTreansfer)
        {
            this.syncTreansfer = syncTreansfer;
        }

        public List<string> SyncNames(ApiControllerParamsInfo param)
        {
            return syncTreansfer.GetNames();
        }
        public async Task<bool> Sync(ApiControllerParamsInfo param)
        {
            string[] names = param.Content.DeJson<string[]>();
            if (names.Length == 1)
            {
                await syncTreansfer.Sync(names[0]);
            }
            else
            {
                syncTreansfer.Sync(names);
            }

            return true;
        }
    }

}
