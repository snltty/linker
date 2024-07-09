using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;
using linker.libs;
using System.Runtime.InteropServices;

namespace linker.updater
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Updater();
            await Helper.Await();
        }


        static void Updater()
        {
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    UpdateInfo updateInfo = GetUpdateInfo();
                    if (updateInfo != null)
                    {

                    }

                    await Task.Delay(15000);
                }


            }, TaskCreationOptions.LongRunning);
        }

        static UpdateInfo GetUpdateInfo()
        {
            try
            {
                using HttpClient httpClient = new HttpClient();
                string str = httpClient.GetStringAsync("http://gh.snltty.com:1808/https://github.com/snltty/linker/releases/latest").Result;
                HtmlDocument hdc = new HtmlDocument();
                hdc.LoadHtml(str);
                string tag = hdc.DocumentNode.QuerySelector("span.css-truncate-target span").InnerText.Trim();

                str = httpClient.GetStringAsync($"http://gh.snltty.com:1808/https://github.com/snltty/linker/releases/expanded_assets/{tag}").Result;
                HtmlDocument hdc1 = new HtmlDocument();
                hdc1.LoadHtml(str);
                string msg = hdc.DocumentNode.QuerySelector(".markdown-body").InnerText.Trim();

                string system = OperatingSystem.IsWindows() ? "win" : OperatingSystem.IsLinux() ? "linux" : "osx";
                string arch = RuntimeInformation.ProcessArchitecture.ToString().ToLower();
                string zip = $"linker-{system}-{arch}.zip";
                var a = hdc1.DocumentNode.QuerySelectorAll("a").FirstOrDefault(c => c.InnerText.Trim() == zip);

                return new UpdateInfo
                {
                    Msg = msg,
                    Tag = tag,
                    Url = $"http://gh.snltty.com:1808/https://github.com{a.GetAttributeValue("href", "").Trim()}"
                };
            }
            catch (Exception)
            {
            }
            return null;
        }


        sealed class UpdateInfo
        {
            public string Tag { get; set; }
            public string Msg { get; set; }
            public string Url { get; set; }
        }
    }
}
