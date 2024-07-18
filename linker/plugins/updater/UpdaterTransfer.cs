using linker.client;
using linker.config;
using linker.libs;
using linker.plugins.updater.messenger;
using linker.server;
using MemoryPack;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace linker.plugins.updater
{
    public sealed class UpdaterTransfer
    {
        private UpdateInfo updateInfo = new UpdateInfo();
        private ConcurrentDictionary<string, UpdateInfo> updateInfos = new ConcurrentDictionary<string, UpdateInfo>();

        private readonly FileConfig fileConfig;
        private readonly MessengerSender messengerSender;
        private readonly ClientSignInState clientSignInState;
        public UpdaterTransfer(FileConfig fileConfig, MessengerSender messengerSender, ClientSignInState clientSignInState)
        {
            this.fileConfig = fileConfig;
            this.messengerSender = messengerSender;
            this.clientSignInState = clientSignInState;

            clientSignInState.NetworkFirstEnabledHandle += () =>
            {
                LoadTask();
                UpdateTask();
            };

            StartClearTempFile();
        }

        /// <summary>
        /// 所有客户端的更新信息
        /// </summary>
        /// <returns></returns>
        public ConcurrentDictionary<string, UpdateInfo> Get()
        {
            return updateInfos;
        }
        /// <summary>
        /// 确认更新
        /// </summary>
        public void Confirm(string version)
        {
            Task.Run(async () =>
            {
                string fileName = Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName);

                await DownloadUpdate(version);
                await ExtractUpdate();

                if (OperatingSystem.IsLinux())
                {
                    string result = CommandHelper.Linux(string.Empty, new string[] { $"chmod a+x {fileName}" });
                }
                else if (OperatingSystem.IsMacOS())
                {
                    string result = CommandHelper.Osx(string.Empty, new string[] { $"chmod a+x {fileName}" });
                }
            });
        }
        /// <summary>
        /// 关闭程序
        /// </summary>
        public void Exit()
        {
            Environment.Exit(1);
        }
        /// <summary>
        /// 来自别的客户端的更新信息
        /// </summary>
        /// <param name="info"></param>
        public void Update(UpdateInfo info)
        {
            if (string.IsNullOrWhiteSpace(info.MachineId) == false)
            {
                updateInfos.AddOrUpdate(info.MachineId, info, (a, b) => info);
            }
        }

        private void UpdateTask()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    if (updateInfo.StatusChanged())
                    {
                        updateInfo.MachineId = fileConfig.Data.Client.Id;
                        await messengerSender.SendOnly(new MessageRequestWrap
                        {
                            Connection = clientSignInState.Connection,
                            MessengerId = (ushort)UpdaterMessengerIds.UpdateForward,
                            Payload = MemoryPackSerializer.Serialize(updateInfo),
                        });
                        Update(updateInfo);
                    }
                    await Task.Delay(1000);
                }
            });
        }
        private void LoadTask()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    await GetUpdateInfo();
                    await Task.Delay(60000);
                }
            });
        }

        private async Task GetUpdateInfo()
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

                Match match = new Regex(@"/snltty/linker/tree/(v[\d.]+)").Match(str);
                string tag = match.Groups[1].Value;
                string[] msg = new Regex(@"<li>(.+)</li>").Matches(str).Select(c => c.Groups[1].Value).ToArray();

                updateInfo.Msg = msg;
                updateInfo.Version = tag;

                updateInfo.Status = UpdateStatus.Checked;
            }
            catch (Exception)
            {
                updateInfo.Status = status;
            }
        }
        private async Task ExtractUpdate()
        {
            //没下载完成
            if (updateInfo.Status != UpdateStatus.Downloaded)
            {
                return;
            }

            UpdateStatus status = updateInfo.Status;
            try
            {
                updateInfo.Status = UpdateStatus.Extracting;
                updateInfo.Current = 0;
                updateInfo.Length = 0;

                using ZipArchive archive = ZipFile.OpenRead("updater.zip");
                updateInfo.Length = archive.Entries.Sum(c => c.Length);

                string configPath = Path.GetFullPath("./configs");

                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string entryPath = Path.GetFullPath(Path.Join("./", entry.FullName.Substring(entry.FullName.IndexOf('/'))));
                    if (entryPath.EndsWith('\\') || entryPath.EndsWith('/'))
                    {
                        continue;
                    }
                    if (entryPath.StartsWith(configPath))
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
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
                updateInfo.Status = status;
            }
        }
        private async Task DownloadUpdate(string version)
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
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
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

        private void StartClearTempFile()
        {
            ClearTempFile();
        }
        private void ClearTempFile(string path = "./")
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
                ClearTempFile(item);
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

        public string MachineId { get; set; }
        public UpdateStatus Status { get; set; } = UpdateStatus.None;
        public long Length { get; set; }
        public long Current { get; set; }


        private int statusCode = 0;
        public bool StatusChanged()
        {
            int code = (byte)Status ^ Length.GetHashCode() ^ Current.GetHashCode();

            bool res = statusCode != code;
            statusCode = code;

            return res;
        }
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
