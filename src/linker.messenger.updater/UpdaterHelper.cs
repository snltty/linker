using linker.libs;
using linker.libs.timer;
using System.Buffers;

namespace linker.messenger.updater
{
    public sealed class UpdaterHelper
    {
        private readonly IUpdaterCommonStore updaterCommonTransfer;
        private readonly IUpdaterInstaller updaterInstaller;
        private UpdaterInfo updateInfo;

        public UpdaterHelper(IUpdaterCommonStore updaterCommonTransfer, IUpdaterInstaller updaterInstaller)
        {
            this.updaterCommonTransfer = updaterCommonTransfer;
            this.updaterInstaller = updaterInstaller;

            updaterInstaller.Clear();
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

                (string datetime, string[] msg, string version) = await updaterInstaller.Check();
                if (string.IsNullOrWhiteSpace(datetime))
                {
                    updateInfo.Status = status;
                    return;
                }

                updateInfo.DateTime = datetime;
                updateInfo.Msg = msg;
                updateInfo.Version = version;
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
        /// <param name="version"></param>
        /// <returns></returns>
        private async Task Download(string version)
        {
            UpdaterStatus status = updateInfo.Status;

            (string url, string savePath) = updaterInstaller.DownloadUrlAndSavePath(version);
            try
            {
                updateInfo.Status = UpdaterStatus.Downloading;
                updateInfo.Current = 0;
                updateInfo.Length = 0;

                if (string.IsNullOrWhiteSpace(url))
                {
                    updateInfo.Status = status;
                    return;
                }

                try
                {
                    File.Delete(savePath);
                }
                catch (Exception)
                {
                }
                LoggerHelper.Instance.Warning($"updater {url}");

                using HttpClient httpClient = new HttpClient();
                using HttpResponseMessage response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                updateInfo.Length = response.Content.Headers.ContentLength ?? 0;

                try
                {
                    if (OperatingSystem.IsAndroid() == false)
                    {
                        DriveInfo drive = new DriveInfo(Path.GetPathRoot(savePath));
                        if (drive.AvailableFreeSpace < updateInfo.Length)
                        {
                            LoggerHelper.Instance.Error($"file size {updateInfo.Length}byte,device free space {drive.AvailableFreeSpace} byte");
                            updateInfo.Status = status;
                            return;
                        }
                    }
                }
                catch (Exception)
                {
                }

                using Stream contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                using FileStream fileStream = new FileStream(savePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                using IMemoryOwner<byte> buffer = MemoryPool<byte>.Shared.Rent(65535);
                int readBytes = 0;
                while ((readBytes = await contentStream.ReadAsync(buffer.Memory).ConfigureAwait(false)) != 0)
                {
                    await fileStream.WriteAsync(buffer.Memory.Slice(0, readBytes)).ConfigureAwait(false);
                    updateInfo.Current += readBytes;
                }

                updateInfo.Status = UpdaterStatus.Downloaded;
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
                try
                {
                    File.Delete(savePath);
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
        /// <returns></returns>
        private async Task Install()
        {
            //没下载完成
            if (updateInfo.Status != UpdaterStatus.Downloaded)
            {
                return;
            }
            UpdaterStatus status = updateInfo.Status;
            try
            {
                updateInfo.Status = UpdaterStatus.Extracting;
                updateInfo.Current = 0;
                updateInfo.Length = 0;

                await updaterInstaller.Install((total, length) =>
                {
                    updateInfo.Length = total;
                    updateInfo.Current += length;
                });

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

            if (this.updateInfo != null && this.updateInfo.Status >= UpdaterStatus.Downloading) return;
            this.updateInfo = updateInfo;

            TimerHelper.Async(async () =>
            {
                await Download(version).ConfigureAwait(false);
                await Install().ConfigureAwait(false);
            });
        }
    }
}
