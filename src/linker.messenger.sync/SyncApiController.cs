using linker.libs.api;
using linker.libs.extends;
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
