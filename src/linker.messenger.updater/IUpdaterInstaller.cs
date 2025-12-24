using linker.libs;
using System.Buffers;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;

namespace linker.messenger.updater
{
    public interface IUpdaterInstaller
    {
        /// <summary>
        /// 检查更新，时间，更新信息，版本
        /// </summary>
        /// <returns></returns>
        public Task<(string, string[], string)> Check();
        /// <summary>
        /// 获取地址。下载地址，保存地址
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        public (string, string) DownloadUrlAndSavePath(string version);
        /// <summary>
        /// 安装
        /// </summary>
        /// <param name="processs">安装进度回调，总长度，当前进度</param>
        /// <returns></returns>
        public Task Install(Action<long, long> processs);
        /// <summary>
        /// 清理文件
        /// </summary>
        public void Clear();
    }

    public class UpdaterInstaller : IUpdaterInstaller
    {
        private readonly IUpdaterCommonStore updaterCommonTransfer;
        public UpdaterInstaller(IUpdaterCommonStore updaterCommonTransfer)
        {
            this.updaterCommonTransfer = updaterCommonTransfer;
        }

        public virtual async Task<(string, string[], string)> Check()
        {
            using CancellationTokenSource cts = new CancellationTokenSource(15000);
            try
            {
                using HttpClientHandler handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                using HttpClient httpClient = new HttpClient(handler);
                string str = await httpClient.GetStringAsync($"{updaterCommonTransfer.UpdateUrl}/version.txt",cts.Token).ConfigureAwait(false);
                string[] arr = str.Split(Environment.NewLine).Select(c => c.Trim('\r').Trim('\n')).ToArray();
                string version = arr[0];
                string datetime = DateTime.Parse(arr[1]).ToString("yyyy-MM-dd HH:mm:ss");
                string[] msg = arr.Skip(2).ToArray();

                return (datetime, msg, version);
            }
            catch (Exception ex)
            {
                cts.Cancel();
                LoggerHelper.Instance.Error(ex);
            }

            return (string.Empty, [], string.Empty);
        }
        public virtual (string, string) DownloadUrlAndSavePath(string version)
        {
            if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                StringBuilder sb = new StringBuilder("linker-");
                sb.Append($"{(OperatingSystem.IsWindows() ? "win" : OperatingSystem.IsLinux() ? "linux" : "osx")}-");
                if (OperatingSystem.IsLinux() && Directory.GetFiles("/lib", "*musl*").Length > 0)
                {
                    sb.Append($"musl-");
                }
                sb.Append(RuntimeInformation.ProcessArchitecture.ToString().ToLower());
                return ($"{updaterCommonTransfer.UpdateUrl}/{version}/{sb.ToString()}.zip", "updater.zip");
            }
            return (string.Empty, string.Empty);
        }
        public virtual async Task Install(Action<long, long> processs)
        {
            if (OperatingSystem.IsWindows() == false && OperatingSystem.IsLinux() == false && OperatingSystem.IsMacOS() == false)
            {
                processs(100, 100);
                return;
            }

            using ZipArchive archive = ZipFile.OpenRead("updater.zip");

            long total = archive.Entries.Sum(c => c.Length);
            processs(total, 0);

            string[] extractExcludeFiles = [];

            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                string entryPath = Path.GetFullPath(Path.Join(Helper.CurrentDirectory, entry.FullName.Substring(entry.FullName.IndexOf('/'))));
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
                using IMemoryOwner<byte> buffer = MemoryPool<byte>.Shared.Rent(4096);
                int bytesRead;
                while ((bytesRead = await entryStream.ReadAsync(buffer.Memory).ConfigureAwait(false)) != 0)
                {
                    await fileStream.WriteAsync(buffer.Memory.Slice(0, bytesRead));
                    processs(total, bytesRead);
                }

                entryStream.Dispose();
                fileStream.Flush();
                fileStream.Dispose();
            }

            archive.Dispose();

            try
            {
                File.Delete("updater.zip");
            }
            catch (Exception)
            {
            }

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
            try
            {
                File.Delete("./linker.ics.exe");
            }
            catch (Exception)
            {
            }
            Helper.AppExit(1);
        }

        public virtual void Clear()
        {
            ClearTempFiles();
        }
        private void ClearTempFiles(string path = "./")
        {
            string fullPath = Path.Join(Helper.CurrentDirectory, path);
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
