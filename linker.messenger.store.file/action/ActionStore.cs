using linker.messenger.action;
namespace linker.messenger.store.file.action
{
    public sealed class ActionStore : IActionStore
    {
        public const string ACTION_ARG_KEY = "ACTION_ARGS";

        public string SignInActionUrl => config.Data.Action.SignInActionUrl;
        public string RelayActionUrl => config.Data.Action.SignInActionUrl;
        public string SForwardActionUrl => config.Data.Action.SignInActionUrl;

        private readonly FileConfig config;
        public ActionStore(FileConfig config)
        {
            this.config = config;
        }

        public void SetActionArg(string action)
        {
            config.Data.Client.Action.Arg = action;
        }
        public void SetActionArgs(Dictionary<string, string> actions)
        {
            config.Data.Client.Action.Args = actions;
            config.Data.Update();
        }
        public bool TryAddActionArg(string host, Dictionary<string, string> args)
        {
            if (string.IsNullOrWhiteSpace(config.Data.Client.Action.Arg) == false)
            {
                args.TryAdd(ACTION_ARG_KEY, config.Data.Client.Action.Arg);
            }
            else if (config.Data.Client.Action.Args.TryGetValue(host, out string arg))
            {
                args.TryAdd(ACTION_ARG_KEY, arg);
            }
            return true;
        }

        public bool TryGetActionArg(Dictionary<string, string> args, out string str, out string machineKey)
        {
            args.TryGetValue("machineKey", out machineKey);
            machineKey = machineKey ?? string.Empty;

            return args.TryGetValue(ACTION_ARG_KEY, out str) && string.IsNullOrWhiteSpace(str) == false;
        }
    }

}