using linker.libs.extends;
using linker.messenger.action;
using System.Collections.Generic;
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

        public void SetActionDynamicArg(string value)
        {
            config.Data.Client.Action.Arg = value;
        }

        public void SetActionStaticArg(string key, string value)
        {
            config.Data.Client.Action.Args.AddOrUpdate(key, value, (a, b) => value);
        }

        public string GetActionStaticArg(string key)
        {
            if (config.Data.Client.Action.Args.TryGetValue(key, out string arg))
            {
                return arg;
            }
            else if (config.Data.Client.Action.Args.Count > 0)
            {
                return config.Data.Client.Action.Args.Values.FirstOrDefault();
            }
            return string.Empty;
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
            else if (config.Data.Client.Action.Args.Count > 0)
            {
                args.TryAdd(ACTION_ARG_KEY, config.Data.Client.Action.Args.Values.FirstOrDefault());
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