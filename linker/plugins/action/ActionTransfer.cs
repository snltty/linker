using linker.config;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
namespace linker.plugins.action
{
    public sealed class ActionTransfer
    {
        public const string ACTION_ARG_KEY = "ACTION_ARGS";


        public string SignInActionUrl => config.Data.Action.SignInActionUrl;
        public string RelayActionUrl => config.Data.Action.SignInActionUrl;
        public string SForwardActionUrl => config.Data.Action.SignInActionUrl;


        private readonly FileConfig config;
        public ActionTransfer(FileConfig config)
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

        public async Task<string> ExcuteActions(string actionJson, string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return string.Empty;
            try
            {
                using HttpClient client = new HttpClient();
                JsonContent json = JsonContent.Create(JsonObject.Parse(actionJson));
                HttpResponseMessage resp = await client.PostAsync(url, json);
                if (resp.IsSuccessStatusCode)
                {
                    string result = await resp.Content.ReadAsStringAsync();
                    if (result.Equals("ok", StringComparison.CurrentCultureIgnoreCase) == false)
                    {
                        return $"post {url} fail->{result}";
                    }
                }
                else
                {
                    return $"post {url} fail->{resp.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                return $"post {url} fail->{ex.Message}";
            }
            return string.Empty;
        }
    }

    public sealed class ActionInfo
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}