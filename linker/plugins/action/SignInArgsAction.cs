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

        public async Task<string> Invoke(string host, Dictionary<string, string> args)
        {
            actionTransfer.TryAddActionArg(host, args);
            await Task.CompletedTask;
            return string.Empty;
        }

        public async Task<string> Verify(SignInfo signInfo, SignCacheInfo cache)
        {
            if (string.IsNullOrWhiteSpace(fileConfig.Data.Action.SignInActionUrl) == false)
            {
                if (actionTransfer.TryGetActionArg(signInfo.Args, out string str) == false)
                {
                    return $"singin action URL exists, but [{signInfo.MachineName}] action value is not configured";
                }
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
                if (actionTransfer.TryGetActionArg(fromMachine.Args, out string str) == false)
                {
                    return $"relay action URL exists, but [{fromMachine.MachineName}] action value is not configured";
                }
                if (toMachine != null && actionTransfer.TryGetActionArg(toMachine.Args, out str) == false)
                {
                    return $"relay action URL exists, but [{toMachine.MachineName}]e action value is not configured";
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
                if (actionTransfer.TryGetActionArg(cache.Args, out string str) == false)
                {
                    return "sforward action URL exists, but action value is not configured";
                }
                return await actionTransfer.ExcuteActions(str, fileConfig.Data.Action.SForwardActionUrl);
            }
            return string.Empty;
        }
    }
}
