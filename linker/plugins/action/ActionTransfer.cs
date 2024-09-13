using linker.libs.extends;
using System.Net.Http.Json;
namespace linker.plugins.action
{
    public sealed class ActionTransfer
    {

        private List<ActionInfo> actions = new List<ActionInfo>();
        public void SetActions(List<ActionInfo> actions)
        {
            this.actions = actions;
        }
        public List<ActionInfo> GetActions()
        {
            return actions;
        }

        public async Task<string> ExcuteActions(List<ActionInfo> actions)
        {
            foreach (var action in actions)
            {
                try
                {
                    using HttpClient client = new HttpClient();
                    JsonContent json = JsonContent.Create(new { Key = action.Key, Value = action.Value });
                    HttpResponseMessage resp = await client.PostAsync(action.Url, json);
                    if (resp.IsSuccessStatusCode)
                    {
                        string result = await resp.Content.ReadAsStringAsync();
                        if (result.Equals("ok", StringComparison.CurrentCultureIgnoreCase) == false)
                        {
                            return $"post {action.Url} fail->{result}";
                        }
                    }
                    else
                    {
                        return $"post {action.Url} fail->{resp.StatusCode}";
                    }
                }
                catch (Exception ex)
                {
                    return $"post {action.Url} fail->{ex.Message}";
                }
            }
            return string.Empty;
        }
    }

    public sealed class ActionInfo
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public string Url { get; set; }
    }
}