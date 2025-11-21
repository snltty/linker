using linker.libs;
using linker.libs.extends;
using linker.libs.web;
using linker.messenger.api;
using linker.messenger.signin;
using linker.tunnel.connection;
using linker.tunnel.transport;

namespace linker.messenger.action
{
    public sealed class ActionApiController : IApiController
    {
        private readonly ActionTransfer actionTransfer;
        private readonly SignInClientState signInClientState;
        private readonly ISignInClientStore signInClientStore;
        private readonly IMessengerSender messengerSender;
        private readonly ISerializer serializer;
        public ActionApiController(ActionTransfer actionTransfer, SignInClientState signInClientState, ISignInClientStore signInClientStore,
            IMessengerSender messengerSender, ISerializer serializer)
        {
            this.actionTransfer = actionTransfer;
            this.signInClientState = signInClientState;
            this.signInClientStore = signInClientStore;
            this.messengerSender = messengerSender;
            this.serializer = serializer;
        }


        [Access(AccessValue.Action)]
        public bool SetArgs(ApiControllerParamsInfo param)
        {
            return actionTransfer.SetActionDynamicArg(param.Content);
        }
        [Access(AccessValue.Action)]
        public async Task<string> GetServerArgs(ApiControllerParamsInfo param)
        {
            if (param.Content == signInClientStore.Id || string.IsNullOrWhiteSpace(param.Content))
            {
                return actionTransfer.GetActionStaticArg();
            }
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)ActionMessengerIds.GetForward,
                Payload = serializer.Serialize(param.Content)
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK && resp.Data.Length > 0)
            {
                return serializer.Deserialize<string>(resp.Data.Span);
            }
            return string.Empty;
        }
        [Access(AccessValue.Action)]
        public async Task<bool> SetServerArgs(ApiControllerParamsInfo param)
        {
            KeyValueInfo<string, string> keyValue = param.Content.DeJson<KeyValueInfo<string,string>>();

            if (keyValue.Key == signInClientStore.Id || string.IsNullOrWhiteSpace(keyValue.Key))
            {
                return actionTransfer.SetActionStaticArg(keyValue.Value);
            }
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)ActionMessengerIds.SetForward,
                Payload = serializer.Serialize(new KeyValuePair<string, string>(keyValue.Key, keyValue.Value))
            }).ConfigureAwait(false);
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }
    }

}
