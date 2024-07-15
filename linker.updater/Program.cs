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
            if (args.Length > 0)
            {
                rootPath = args[0];
            }
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
                            if (NeedDownload())
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
                return File.Exists(Path.Join(rootPath, "updater.zip")) && File.Exists(Path.Join(rootPath, "extract.txt"));
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
                string[] command = File.ReadAllText(Path.Join(rootPath, "extract.txt")).Split(Environment.NewLine);
                CommandHelper.Execute(string.Empty, new string[] { command[0] });

                ZipFile.ExtractToDirectory(Path.Join(rootPath, "updater.zip"), Path.Join(rootPath, "../"), Encoding.UTF8, true);

                File.Delete(Path.Join(rootPath, "extract.txt"));
                File.Delete(Path.Join(rootPath, "updater.zip"));

                CommandHelper.Execute(string.Empty, new string[] { command[1] });
            }
            catch (Exception)
            {
            }
        }

        static bool NeedDownload()
        {
            try
            {
                return File.Exists(Path.Join(rootPath, "extract.txt"));
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


                File.WriteAllText(Path.Join(rootPath, "version.txt"), tag);
                File.WriteAllText(Path.Join(rootPath, "msg.txt"), msg);

                return new UpdateInfo
                {
                    Msg = msg,
                    Version = tag,
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
            public string Version { get; set; }
            public string Msg { get; set; }
            public string Url { get; set; }
        }
    }


    public sealed class CommandHelper
    {

        public static string Execute(string arg, string[] commands, bool readResult = true)
        {
            if (OperatingSystem.IsWindows())
            {
                return Windows(arg, commands, readResult);
            }
            else if (OperatingSystem.IsLinux())
            {
                return Linux(arg, commands, readResult);
            }
            return Osx(arg, commands, readResult);
        }
        public static string Windows(string arg, string[] commands, bool readResult = true)
        {
            return Execute("cmd.exe", arg, commands, readResult);
        }
        public static string Linux(string arg, string[] commands, bool readResult = true)
        {
            return Execute("/bin/bash", arg, commands, readResult);
        }
        public static string Osx(string arg, string[] commands, bool readResult = true)
        {
            return Execute("/bin/bash", arg, commands, readResult);
        }

        public static Process Execute(string fileName, string arg)
        {
            Process proc = new Process();
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.FileName = fileName;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.RedirectStandardInput = true;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.Arguments = arg;
            proc.StartInfo.Verb = "runas";
            proc.Start();

            //Process proc = Process.Start(fileName, arg);
            return proc;
        }

        public static string Execute(string fileName, string arg, string[] commands, bool readResult = true)
        {
            using Process proc = new Process();
            proc.StartInfo.WorkingDirectory = Path.GetFullPath(Path.Join("./"));
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.FileName = fileName;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.RedirectStandardInput = true;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.Arguments = arg;
            proc.StartInfo.Verb = "runas";
            proc.Start();

            if (commands.Length > 0)
            {
                for (int i = 0; i < commands.Length; i++)
                {
                    proc.StandardInput.WriteLine(commands[i]);
                }
            }
            proc.StandardInput.AutoFlush = true;
            if (readResult)
            {
                proc.StandardInput.WriteLine("exit");
                proc.StandardInput.Close();
                string output = proc.StandardOutput.ReadToEnd();
                string error = proc.StandardError.ReadToEnd();
                proc.WaitForExit();
                proc.Close();
                proc.Dispose();

                return output;
            }
            proc.StandardOutput.Read();
            proc.Close();
            proc.Dispose();
            return string.Empty;
        }
    }
}
