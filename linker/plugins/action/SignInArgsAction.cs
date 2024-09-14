using linker.config;
using linker.plugins.relay.validator;
using linker.plugins.sforward.config;
using linker.plugins.sforward.validator;
using linker.plugins.signin.messenger;
using linker.plugins.signIn.args;

namespace linker.plugins.action
{
    public sealed class SignInArgsAction : ISignInArgs
    {
        private readonly ActionTransfer actionTransfer;
        private readonly FileConfig fileConfig;

        public SignInArgsAction(ActionTransfer actionTransfer, FileConfig fileConfig)
        {
            this.actionTransfer = actionTransfer;
            this.fileConfig = fileConfig;
        }

        public async Task<string> Invoke(Dictionary<string, string> args)
        {
            args.TryAdd(ActionTransfer.ACTION_ARG_KEY, actionTransfer.GetAction());

            await Task.CompletedTask;
            return string.Empty;
        }

        public async Task<string> Verify(SignInfo signInfo, SignCacheInfo cache)
        {
            if (signInfo.Args.TryGetValue(ActionTransfer.ACTION_ARG_KEY, out string str))
            {
                return await actionTransfer.ExcuteActions(str, fileConfig.Data.Action.SignInActionUrl);
            }

            return string.Empty;
        }
    }

    public sealed class RelayValidatorAction : IRelayValidator
    {
        private readonly ActionTransfer actionTransfer;
        private readonly FileConfig fileConfig;

        public RelayValidatorAction(ActionTransfer actionTransfer, FileConfig fileConfig)
        {
            this.actionTransfer = actionTransfer;
            this.fileConfig = fileConfig;
        }

        public async Task<string> Validate(SignCacheInfo fromMachine, SignCacheInfo toMachine)
        {
            if (string.IsNullOrWhiteSpace(fileConfig.Data.Action.RelayActionUrl) == false)
            {
                if (fromMachine.Args.TryGetValue(ActionTransfer.ACTION_ARG_KEY, out string str) == false || string.IsNullOrWhiteSpace(str))
                {
                    return $"action URL exists, but [{fromMachine.MachineName}] action value is not configured";
                }
                if (toMachine != null && toMachine.Args.TryGetValue(ActionTransfer.ACTION_ARG_KEY, out str) == false || string.IsNullOrWhiteSpace(str))
                {
                    return $"action URL exists, but [{toMachine.MachineName}]e action value is not configured";
                }
                return await actionTransfer.ExcuteActions(str, fileConfig.Data.Action.RelayActionUrl);
            }
            return string.Empty;
        }
    }

    public sealed class SForwardValidatorAction : ISForwardValidator
    {
        private readonly ActionTransfer actionTransfer;
        private readonly FileConfig fileConfig;

        public SForwardValidatorAction(ActionTransfer actionTransfer, FileConfig fileConfig)
        {
            this.actionTransfer = actionTransfer;
            this.fileConfig = fileConfig;
        }

        public async Task<string> Validate(SignCacheInfo cache, SForwardAddInfo sForwardAddInfo)
        {
            if (string.IsNullOrWhiteSpace(fileConfig.Data.Action.SForwardActionUrl) == false)
            {
                if (cache.Args.TryGetValue(ActionTransfer.ACTION_ARG_KEY, out string str) == false || string.IsNullOrWhiteSpace(str))
                {
                    return "action URL exists, but action value is not configured";
                }
                return await actionTransfer.ExcuteActions(str, fileConfig.Data.Action.SForwardActionUrl);
            }
            return string.Empty;
        }
    }
}
