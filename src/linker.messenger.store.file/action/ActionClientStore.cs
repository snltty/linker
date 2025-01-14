using linker.messenger.action;
namespace linker.messenger.store.file.action
{
    public sealed class ActionClientStore : IActionClientStore
    {
        public const string ACTION_ARG_KEY = "SNLTTY_ACTION_ARGS";

        private readonly FileConfig config;
        public ActionClientStore(FileConfig config)
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
        public bool Confirm()
        {
            config.Data.Update();
            return true;
        }
    }

}