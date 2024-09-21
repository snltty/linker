using linker.libs.api;
using linker.plugins.capi;
using linker.plugins.action;
using linker.plugins.client;
using linker.libs.extends;
using linker.config;

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


        [ClientApiAccessAttribute(ClientApiAccess.Action)]
        public bool SetArgs(ApiControllerParamsInfo param)
        {
            actionTransfer.SetActionArg(param.Content);
            clientSignInTransfer.SignOut();
            _ = clientSignInTransfer.SignIn();

            return true;
        }

        [ClientApiAccessAttribute(ClientApiAccess.Action)]
        public bool SetServerArgs(ApiControllerParamsInfo param)
        {
            actionTransfer.SetActionArgs(param.Content.DeJson<Dictionary<string, string>>());
            clientSignInTransfer.SignOut();
            _ = clientSignInTransfer.SignIn();
            return true;
        }
    }

}
