using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;
using linker.libs;
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;

namespace linker.updater
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Updater();
            await Helper.Await();
        }

        static string rootPath = "./updater";
        static void Updater()
        {
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    try
                    {
                        UpdateInfo updateInfo = GetUpdateInfo();
                        if (updateInfo != null)
                        {
                            if (NeedDownload(updateInfo))
                            {
                                await DownloadUpdate(updateInfo);
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }
                    try
                    {
                        if (NeedExtract())
                        {
                            ExtractUpdate();
                        }
                    }
                    catch (Exception)
                    {
                    }

                    await Task.Delay(15000);
                }


            }, TaskCreationOptions.LongRunning);
        }
        static bool NeedExtract()
        {
            try
            {
                return File.Exists(Path.Join(rootPath, "version.txt"))
                    && File.Exists(Path.Join(rootPath,"updater.zip"))
                    && File.Exists(Path.Join(rootPath, "extract.txt"))
                    && File.ReadAllText(Path.Join(rootPath, "version.txt")) != $"v{FileVersionInfo.GetVersionInfo("linker.exe").FileVersion}";
            }
            catch (Exception)
            {
            }
            return false;
        }
        static void ExtractUpdate()
        {
            try
            {
                ZipFile.ExtractToDirectory(Path.Join(rootPath, "updater.zip"), "./", Encoding.UTF8, true);

                File.Delete(Path.Join(rootPath, "version.txt"));
                File.Delete(Path.Join(rootPath, "msg.txt"));
                File.Delete(Path.Join(rootPath, "extract.txt"));
                File.Delete(Path.Join(rootPath, "updater.zip"));
            }
            catch (Exception)
            {
            }
        }

        static bool NeedDownload(UpdateInfo updateInfo)
        {
            try
            {
                return true;
                return (File.Exists(Path.Join(rootPath, "version.txt")) == false
                    || File.ReadAllText(Path.Join(rootPath, "version.txt")) != updateInfo.Tag)
                    && $"v{FileVersionInfo.GetVersionInfo("linker.exe").FileVersion}" != updateInfo.Tag;
            }
            catch (Exception)
            {
            }
            return false;
        }
        static async Task DownloadUpdate(UpdateInfo updateInfo)
        {
            try
            {
                if (Directory.Exists(rootPath) == false)
                {
                    Directory.CreateDirectory(rootPath);
                }

                using FileStream fileStream = new FileStream(Path.Join(rootPath, "updater.zip"), FileMode.OpenOrCreate, FileAccess.ReadWrite);
                using HttpClient httpClient = new HttpClient();
                using Stream stream = await httpClient.GetStreamAsync(updateInfo.Url);
                await stream.CopyToAsync(fileStream);

                fileStream.Flush();
                fileStream.Close();
                fileStream.Dispose();

                File.WriteAllText(Path.Join(rootPath, "version.txt"), updateInfo.Tag);
                File.WriteAllText(Path.Join(rootPath, "msg.txt"), updateInfo.Msg);
            }
            catch (Exception)
            {
            }
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
