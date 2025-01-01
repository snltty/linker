using linker.libs.api;
using linker.libs.extends;
using linker.messenger.api;

namespace linker.messenger.action
{
    public sealed class ActionApiController : IApiController
    {
        private readonly IActionStore actionStore;

        public ActionApiController(IActionStore actionStore)
        {
            this.actionStore = actionStore;
        }


        [Access(AccessValue.Action)]
        public bool SetArgs(ApiControllerParamsInfo param)
        {
            actionStore.SetActionArg(param.Content);
            return true;
        }
        [Access(AccessValue.Action)]
        public bool SetServerArgs(ApiControllerParamsInfo param)
        {
            actionStore.SetActionArgs(param.Content.DeJson<Dictionary<string, string>>());
            return true;
        }
    }

}
