using linker.messenger.action;
namespace linker.messenger.store.file.action
{
    public sealed class ActionServerStore : IActionServerStore
    {
        public const string ACTION_ARG_KEY = "SNLTTY_ACTION_ARGS";

        public string SignInActionUrl => config.Data.Action.SignInActionUrl;
        public string RelayActionUrl => config.Data.Action.RelayActionUrl;
        public string RelayNodeUrl => config.Data.Action.RelayNodeUrl;
        public string SForwardActionUrl => config.Data.Action.SForwardActionUrl;
      

        private readonly FileConfig config;
        public ActionServerStore(FileConfig config)
        {
            this.config = config;
        }

        public bool SetSignInActionUrl(string url)
        {
            config.Data.Action.SignInActionUrl = url;
            return true;
        }

        public bool SetRelayActionUrl(string url)
        {
            config.Data.Action.RelayActionUrl = url;
            return true;
        }
        public bool SetRelayNodeUrl(string url)
        {
            config.Data.Action.RelayNodeUrl = url;
            return true;
        }

        public bool SetSForwardActionUrl(string url)
        {
            config.Data.Action.SForwardActionUrl = url;
            return true;
        }

        public bool TryGetActionArg(Dictionary<string, string> args, out string str, out string machineKey)
        {
            args.TryGetValue("machineKey", out machineKey);
            machineKey = machineKey ?? string.Empty;

            return args.TryGetValue(ACTION_ARG_KEY, out str) && string.IsNullOrWhiteSpace(str) == false;
        }

        public bool Confirm()
        {
            config.Data.Update();
            return true;
        }

        
    }

}