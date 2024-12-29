using System.Net.Http.Json;
using System.Text.Json.Nodes;
namespace linker.messenger.action
{
    public sealed class ActionTransfer
    {
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
}