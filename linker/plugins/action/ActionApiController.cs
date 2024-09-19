using linker.libs.api;
using linker.plugins.capi;
using linker.plugins.action;
using linker.plugins.client;

namespace linker.plugins.signin
{
    public sealed class ActionApiController : IApiClientController
    {
        private readonly ActionTransfer actionTransfer;
        private readonly ClientSignInTransfer clientSignInTransfer;

        public ActionApiController(ActionTransfer actionTransfer, ClientSignInTransfer clientSignInTransfer)
        {
            this.actionTransfer = actionTransfer;
            this.clientSignInTransfer = clientSignInTransfer;
        }

        public bool SetArgs(ApiControllerParamsInfo param)
        {
            actionTransfer.SetActionArg(param.Content);
            clientSignInTransfer.SignOut();
            _ = clientSignInTransfer.SignIn();

            return true;
        }
    }

}
