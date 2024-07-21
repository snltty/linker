using linker.libs;
using MemoryPack;
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace linker.plugins.updater
{
    public sealed class UpdaterHelper
    {
        private string[] extractExcludeFiles = ["msquic.dll", "msquic-openssl.dll", "tun2socks", "tun2socks.exe"];

        public UpdaterHelper()
        {
            ClearFiles();
        }

        /// <summary>
        /// 获取更新信息
        /// </summary>
        /// <param name="updateInfo"></param>
        /// <returns></returns>
        public async Task GetUpdateInfo(UpdateInfo updateInfo)
        {
            //正在检查，或者已经确认更新了
            if (updateInfo.Status == UpdateStatus.Checking || updateInfo.Status > UpdateStatus.Checked)
            {
                return;
            }

            UpdateStatus status = updateInfo.Status;
            try
            {
                updateInfo.Status = UpdateStatus.Checking;

                using HttpClient httpClient = new HttpClient();
                string str = await httpClient.GetStringAsync("http://gh.snltty.com:1808/https://github.com/snltty/linker/releases/latest").WaitAsync(TimeSpan.FromSeconds(15));

                string datetime = DateTime.Parse(new Regex("datetime=\"(.+)\"").Match(str).Groups[1].Value).ToString("yyyy-MM-dd HH:mm:ss");
                string tag = new Regex(@"/snltty/linker/tree/(v[\d.]+)").Match(str).Groups[1].Value;
                string[] msg = new Regex(@"<li>(.+)</li>").Matches(str).Select(c => c.Groups[1].Value).ToArray();

                updateInfo.DateTime = datetime;
                updateInfo.Msg = msg;
                updateInfo.Version = tag;

                updateInfo.Status = UpdateStatus.Checked;
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
                updateInfo.Status = status;
            }
        }
        /// <summary>
        /// 下载更新
        /// </summary>
        /// <param name="updateInfo"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        public async Task DownloadUpdate(UpdateInfo updateInfo, string version)
        {
            if (updateInfo.Status != UpdateStatus.Checked)
            {
                return;
            }
            UpdateStatus status = updateInfo.Status;
            try
            {
                updateInfo.Status = UpdateStatus.Downloading;
                updateInfo.Current = 0;
                updateInfo.Length = 0;

                string system = OperatingSystem.IsWindows() ? "win" : OperatingSystem.IsLinux() ? "linux" : "osx";
                string arch = RuntimeInformation.ProcessArchitecture.ToString().ToLower();
                string url = $"http://gh.snltty.com:1808/https://github.com/snltty/linker/releases/download/{version}/linker-{system}-{arch}.zip";

                using HttpClient httpClient = new HttpClient();
                using HttpResponseMessage response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                updateInfo.Length = response.Content.Headers.ContentLength ?? 0;
                using Stream contentStream = await response.Content.ReadAsStreamAsync();


                using FileStream fileStream = new FileStream("updater.zip", FileMode.OpenOrCreate, FileAccess.ReadWrite);
                byte[] buffer = new byte[4096];
                int readBytes = 0;
                while ((readBytes = await contentStream.ReadAsync(buffer)) != 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, readBytes));
                    updateInfo.Current += readBytes;
                }

                updateInfo.Status = UpdateStatus.Downloaded;
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
                try
                {
                    File.Delete("updater.zip");
                }
                catch (Exception)
                {
                }
                updateInfo.Status = status;
            }
        }
        /// <summary>
        /// 解压更新
        /// </summary>
        /// <param name="updateInfo"></param>
        /// <returns></returns>
        public async Task ExtractUpdate(UpdateInfo updateInfo)
        {
            //没下载完成
            if (updateInfo.Status != UpdateStatus.Downloaded)
            {
                return;
            }
            string fileName = Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName);
            UpdateStatus status = updateInfo.Status;
            try
            {
                updateInfo.Status = UpdateStatus.Extracting;
                updateInfo.Current = 0;
                updateInfo.Length = 0;

                using ZipArchive archive = ZipFile.OpenRead("updater.zip");
                updateInfo.Length = archive.Entries.Sum(c => c.Length);

                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string entryPath = Path.GetFullPath(Path.Join("./", entry.FullName.Substring(entry.FullName.IndexOf('/'))));
                    if (entryPath.EndsWith('\\') || entryPath.EndsWith('/'))
                    {
                        continue;
                    }
                    if (extractExcludeFiles.Contains(Path.GetFileName(entryPath)))
                    {
                        continue;
                    }

                    if (Directory.Exists(Path.GetDirectoryName(entryPath)) == false)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(entryPath));
                    }
                    if (File.Exists(entryPath))
                    {
                        try
                        {
                            File.Move(entryPath, $"{entryPath}.temp", true);
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }

                    using Stream entryStream = entry.Open();
                    using FileStream fileStream = File.Create(entryPath);
                    byte[] buffer = new byte[4096];
                    int bytesRead;
                    while ((bytesRead = await entryStream.ReadAsync(buffer)) != 0)
                    {
                        await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                        updateInfo.Current += bytesRead;
                    }

                    entryStream.Dispose();
                    fileStream.Flush();
                    fileStream.Dispose();
                }

                archive.Dispose();
                File.Delete("updater.zip");

                updateInfo.Status = UpdateStatus.Extracted;
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
                updateInfo.Status = status;
            }
        }

        /// <summary>
        /// 提交更新，开始下载和解压
        /// </summary>
        /// <param name="updateInfo"></param>
        /// <param name="version"></param>
        public void Confirm(UpdateInfo updateInfo, string version)
        {
            Task.Run(async () =>
            {
                await DownloadUpdate(updateInfo, version);
                await ExtractUpdate(updateInfo);
                Environment.Exit(1);
            });
        }

        /// <summary>
        /// 清理旧文件
        /// </summary>
        private void ClearFiles()
        {
            ClearTempFiles();
        }
        private void ClearTempFiles(string path = "./")
        {
            string fullPath = Path.GetFullPath(path);

            foreach (var item in Directory.GetFiles(fullPath).Where(c => c.EndsWith(".temp")))
            {
                try
                {
                    File.Delete(item);
                }
                catch (Exception)
                {
                }
            }
            foreach (var item in Directory.GetDirectories(fullPath))
            {
                ClearTempFiles(item);
            }
        }
    }

    [MemoryPackable]
    public sealed partial class UpdateInfo
    {
        [MemoryPackIgnore]
        public string Version { get; set; }
        [MemoryPackIgnore]
        public string[] Msg { get; set; }
        [MemoryPackIgnore]
        public string DateTime { get; set; }

        public string MachineId { get; set; }

        private ulong counter = 0;
        [MemoryPackIgnore, JsonIgnore]
        public bool Updated => Interlocked.And(ref counter, 0x0) > 0;


        private UpdateStatus status = UpdateStatus.None;
        public UpdateStatus Status { get => status; set { status = value; Interlocked.Increment(ref counter); } }

        private long length = 0;
        public long Length { get => length; set { length = value; Interlocked.Increment(ref counter); } }
        private long current = 0;
        public long Current { get => current; set { current = value; Interlocked.Increment(ref counter); } }
    }

    public enum UpdateStatus : byte
    {
        None = 0,
        Checking = 1,
        Checked = 2,
        Downloading = 3,
        Downloaded = 4,
        Extracting = 5,
        Extracted = 6
    }
}
