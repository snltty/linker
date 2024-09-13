using linker.libs.extends;
using linker.plugins.signin.messenger;
using linker.plugins.signIn.args;

namespace linker.plugins.action
{
    public sealed class SignInArgsAction : ISignInArgs
    {
        private readonly ActionTransfer actionTransfer;
        public SignInArgsAction(ActionTransfer actionTransfer)
        {
            this.actionTransfer = actionTransfer;
        }

        public async Task<string> Invoke(Dictionary<string, string> args)
        {
            args.TryAdd("ACTION_ARGS", actionTransfer.GetActions().ToJson());

            await Task.CompletedTask;
            return string.Empty;
        }

        public async Task<string> Verify(SignInfo signInfo, SignCacheInfo cache)
        {
            if (signInfo.Args.TryGetValue("ACTION_ARGS", out string str))
            {
                return await actionTransfer.ExcuteActions(str.DeJson<List<ActionInfo>>());
            }

            return string.Empty;
        }
    }
}
