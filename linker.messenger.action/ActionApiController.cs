using linker.libs.api;
using linker.libs.extends;

namespace linker.messenger.action
{
    public sealed class ActionApiController : IApiController
    {
        private readonly IActionStore actionStore;

        public ActionApiController(IActionStore actionStore)
        {
            this.actionStore = actionStore;
        }


        public bool SetArgs(ApiControllerParamsInfo param)
        {
            actionStore.SetActionArg(param.Content);
            return true;
        }

        public bool SetServerArgs(ApiControllerParamsInfo param)
        {
            actionStore.SetActionArgs(param.Content.DeJson<Dictionary<string, string>>());
            return true;
        }
    }

}
