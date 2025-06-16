using linker.messenger.signin;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
namespace linker.messenger.action
{
    public sealed class ActionTransfer
    {
        private readonly ISignInClientStore signInClientStore;
        private readonly IActionClientStore actionClientStore;
        public ActionTransfer(ISignInClientStore signInClientStore, IActionClientStore actionClientStore)
        {
            this.signInClientStore = signInClientStore;
            this.actionClientStore = actionClientStore;
        }
        public async Task<string> ExcuteActions(string actionJson, string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return string.Empty;
            try
            {
                using HttpClient client = new HttpClient();
                JsonContent json = JsonContent.Create(JsonObject.Parse(actionJson));
                HttpResponseMessage resp = await client.PostAsync(url, json).ConfigureAwait(false);
                if (resp.IsSuccessStatusCode)
                {
                    string result = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
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

        public bool SetActionDynamicArg(string value)
        {
            actionClientStore.SetActionDynamicArg(value);
            return actionClientStore.Confirm();
        }
        public bool SetActionStaticArg(string value)
        {
            actionClientStore.SetActionStaticArg(signInClientStore.Server.Host, value);
            return actionClientStore.Confirm();
        }
        public string GetActionStaticArg()
        {
            return actionClientStore.GetActionStaticArg(signInClientStore.Server.Host);
        }
        public bool TryAddActionArg(Dictionary<string, string> args)
        {
            return actionClientStore.TryAddActionArg(signInClientStore.Server.Host, args);
        }
    }
}