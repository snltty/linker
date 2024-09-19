using linker.config;
using linker.libs;
using linker.libs.extends;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
namespace linker.plugins.action
{
    public sealed class ActionTransfer
    {
        public const string ACTION_ARG_KEY = "ACTION_ARGS";
        private string action = new ActionInfo { Key = "token", Value = Helper.GlobalString }.ToJson();

        public void SetActionArg(string action)
        {
            this.action = action;
        }
        public string GetActionArg()
        {
            return action;
        }

        public bool TryGetActionArg(Dictionary<string, string> args, out string str)
        {
            if (args.TryGetValue(ACTION_ARG_KEY, out str) == false || string.IsNullOrWhiteSpace(str))
            {
                args.TryGetValue("signin-arg", out str);
            }
            return string.IsNullOrWhiteSpace(str) == false;
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