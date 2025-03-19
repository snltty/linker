using linker.libs;
using linker.libs.timer;
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;

namespace linker.messenger.updater
{
    public sealed class UpdaterHelper
    {
        private string[] extractExcludeFiles = [];

        private readonly IUpdaterCommonStore updaterCommonTransfer;
        public UpdaterHelper(IUpdaterCommonStore updaterCommonTransfer)
        {
            this.updaterCommonTransfer = updaterCommonTransfer;
            ClearFiles();
        }

        /// <summary>
        /// 获取更新信息
        /// </summary>
        /// <param name="updateInfo"></param>
        /// <returns></returns>
        public async Task GetUpdateInfo(UpdaterInfo updateInfo)
        {
            //正在检查，或者已经确认更新了
            if (updateInfo.Status == UpdaterStatus.Checking || updateInfo.Status > UpdaterStatus.Checked)
            {
                return;
            }
            UpdaterStatus status = updateInfo.Status;
            try
            {
                updateInfo.Status = UpdaterStatus.Checking;
                using HttpClientHandler handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                using HttpClient httpClient = new HttpClient(handler);
                string str = await httpClient.GetStringAsync($"{updaterCommonTransfer.UpdateUrl}/version.txt").WaitAsync(TimeSpan.FromSeconds(15)).ConfigureAwait(false);

                string[] arr = str.Split(Environment.NewLine).Select(c => c.Trim('\r').Trim('\n')).ToArray();

                string datetime = DateTime.Parse(arr[1]).ToString("yyyy-MM-dd HH:mm:ss");
                string tag = arr[0];
                string[] msg = arr.Skip(2).ToArray();

                updateInfo.DateTime = datetime;
                updateInfo.Msg = msg;
                updateInfo.Version = tag;

                updateInfo.Status = UpdaterStatus.Checked;
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
        public async Task DownloadUpdate(UpdaterInfo updateInfo, string version)
        {
            UpdaterStatus status = updateInfo.Status;
            try
            {
                updateInfo.Status = UpdaterStatus.Downloading;
                updateInfo.Current = 0;
                updateInfo.Length = 0;

                StringBuilder sb = new StringBuilder("linker-");
                sb.Append($"{(OperatingSystem.IsWindows() ? "win" : OperatingSystem.IsLinux() ? "linux" : "osx")}-");
                if (OperatingSystem.IsLinux() && Directory.GetFiles("/lib", "*musl*").Length > 0)
                {
                    sb.Append($"musl-");
                }
                sb.Append(RuntimeInformation.ProcessArchitecture.ToString().ToLower());

                string url = $"{updaterCommonTransfer.UpdateUrl}/{version}/{sb.ToString()}.zip";
                LoggerHelper.Instance.Warning($"updater {url}");

                using HttpClient httpClient = new HttpClient();
                using HttpResponseMessage response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                updateInfo.Length = response.Content.Headers.ContentLength ?? 0;
                using Stream contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);


                using FileStream fileStream = new FileStream("updater.zip", FileMode.OpenOrCreate, FileAccess.ReadWrite);
                byte[] buffer = new byte[4096];
                int readBytes = 0;
                while ((readBytes = await contentStream.ReadAsync(buffer).ConfigureAwait(false)) != 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, readBytes)).ConfigureAwait(false);
                    updateInfo.Current += readBytes;
                }

                updateInfo.Status = UpdaterStatus.Downloaded;
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
        public async Task ExtractUpdate(UpdaterInfo updateInfo)
        {
            //没下载完成
            if (updateInfo.Status != UpdaterStatus.Downloaded)
            {
                return;
            }
            string fileName = Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName);
            UpdaterStatus status = updateInfo.Status;
            try
            {
                updateInfo.Status = UpdaterStatus.Extracting;
                updateInfo.Current = 0;
                updateInfo.Length = 0;

                using ZipArchive archive = ZipFile.OpenRead("updater.zip");
                updateInfo.Length = archive.Entries.Sum(c => c.Length);


                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string entryPath = Path.GetFullPath(Path.Join(Helper.currentDirectory, entry.FullName.Substring(entry.FullName.IndexOf('/'))));
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
                    while ((bytesRead = await entryStream.ReadAsync(buffer).ConfigureAwait(false)) != 0)
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

                updateInfo.Status = UpdaterStatus.Extracted;
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
        public void Confirm(UpdaterInfo updateInfo, string version)
        {
            if (string.IsNullOrWhiteSpace(version)) return;

            TimerHelper.Async(async () =>
            {
                await DownloadUpdate(updateInfo, version).ConfigureAwait(false);
                await ExtractUpdate(updateInfo).ConfigureAwait(false);

                if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
                {
                    try
                    {
                        File.SetUnixFileMode("./linker", UnixFileMode.GroupExecute | UnixFileMode.OtherExecute | UnixFileMode.UserExecute);
                    }
                    catch (Exception)
                    {
                    }
                }
                try
                {
                    File.Delete("./linker.service.exe");
                }
                catch (Exception)
                {
                }
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
            string fullPath = Path.Join(Helper.currentDirectory,path);
            if (Directory.Exists(fullPath))
            {
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
    }
}
