using linker.libs.api;
using linker.libs.extends;
using linker.plugins.capi;
using linker.plugins.action;

namespace linker.plugins.signin
{
    public sealed class ActionApiController : IApiClientController
    {
        private readonly ActionTransfer actionTransfer;

        public ActionApiController(ActionTransfer actionTransfer)
        {
            this.actionTransfer = actionTransfer;
        }

        public void SetArgs(ApiControllerParamsInfo param)
        {
            actionTransfer.SetActions(param.Content.DeJson<List<ActionInfo>>());
        }
    }

}
