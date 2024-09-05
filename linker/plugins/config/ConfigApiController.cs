using linker.config;
using linker.libs.api;
using linker.libs.extends;
using linker.client.config;
using linker.plugins.capi;
using System.IO.Compression;
using linker.libs;

namespace linker.plugins.config
{
    public sealed class ConfigClientApiController : IApiClientController
    {
        private readonly RunningConfig runningConfig;
        private readonly FileConfig config;

        public ConfigClientApiController(RunningConfig runningConfig, FileConfig config)
        {
            this.runningConfig = runningConfig;
            this.config = config;

            ClearTask();
        }

        public object Get(ApiControllerParamsInfo param)
        {
            return new { Common = config.Data.Common, Client = config.Data.Client, Server = config.Data.Server, Running = runningConfig.Data };
        }

        public bool Install(ApiControllerParamsInfo param)
        {
            ConfigInstallInfo info = param.Content.DeJson<ConfigInstallInfo>();

            if (info.Common.Modes.Contains("client"))
            {
                config.Data.Client.Name = info.Client.Name;
                config.Data.Client.GroupId = info.Client.GroupId;
                config.Data.Client.CApi.WebPort = info.Client.Web;
                config.Data.Client.CApi.ApiPort = info.Client.Api;
                config.Data.Client.CApi.ApiPassword = info.Client.Password;

                if (info.Client.HasServer)
                {
                    config.Data.Client.Server = info.Client.Server;
                    runningConfig.Data.SForwardSecretKey = info.Client.SForwardSecretKey;
                    runningConfig.Data.UpdaterSecretKey = info.Client.UpdaterSecretKey;
                    foreach (var item in runningConfig.Data.Relay.Servers)
                    {
                        item.SecretKey = info.Client.RelaySecretKey;
                    }
                    foreach (var item in runningConfig.Data.Tunnel.Servers)
                    {
                        item.Host = info.Client.Server;
                    }
                    foreach (var item in runningConfig.Data.Client.Servers)
                    {
                        item.Host = info.Client.Server;
                    }
                }
            }
            if (info.Common.Modes.Contains("server"))
            {
                config.Data.Server.ServicePort = info.Server.ServicePort;
                config.Data.Server.Relay.SecretKey = info.Server.Relay.SecretKey;

                config.Data.Server.SForward.SecretKey = info.Server.SForward.SecretKey;
                config.Data.Server.SForward.WebPort = info.Server.SForward.WebPort;
                config.Data.Server.SForward.TunnelPortRange = info.Server.SForward.TunnelPortRange;

                config.Data.Server.Updater.SecretKey = info.Server.Updater.SecretKey;
            }

            config.Data.Common.Modes = info.Common.Modes;
            config.Data.Common.Install = true;
            config.Data.Update();

            runningConfig.Data.Update();

            return true;
        }


        public bool Export(ApiControllerParamsInfo param)
        {
            try
            {
                string dirName = "client-node-export";
                string rootPath = Path.GetFullPath($"./web/{dirName}");
                string zipPath = Path.GetFullPath($"./web/{dirName}.zip");

                try
                {
                    File.Delete(zipPath);
                }
                catch (Exception)
                {
                }
                DeleteDirectory(rootPath);
                CopyDirectory(Path.GetFullPath("./"), rootPath, dirName);
                DeleteDirectory(Path.Combine(rootPath, $"configs"));
                DeleteDirectory(Path.Combine(rootPath, $"logs"));
                DeleteDirectory(Path.Combine(rootPath, $"web"));

                string configPath = Path.Combine(rootPath, $"configs");
                Directory.CreateDirectory(configPath);
                ConfigClientInfo client = config.Data.Client.ToJson().DeJson<ConfigClientInfo>();
                client.Name = string.Empty;
                client.Id = string.Empty;
                client.CApi.WebPort = 0;
                client.OnlyNode = true;
                File.WriteAllText(Path.Combine(configPath, $"client.json"), client.Set(client));


                ConfigCommonInfo common = config.Data.Common.ToJson().DeJson<ConfigCommonInfo>();
                common.Install = true;
                common.Modes = ["client"];
                File.WriteAllText(Path.Combine(configPath, $"common.json"), common.ToJsonFormat());


                ZipFile.CreateFromDirectory(rootPath, zipPath);
                DeleteDirectory(rootPath);
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
            }
            return true;
        }
        private void DeleteDirectory(string sourceDir)
        {
            if (Directory.Exists(sourceDir) == false) return;

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                File.Delete(Path.Combine(sourceDir, file));
            }
            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                DeleteDirectory(Path.Combine(sourceDir, subDir));
            }
            Directory.Delete(sourceDir);
        }
        private void CopyDirectory(string sourceDir, string destDir, string excludeDir)
        {
            // 创建目标目录
            Directory.CreateDirectory(destDir);

            // 复制所有文件
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(destDir, fileName);
                File.Copy(file, destFile, true); // true 表示如果目标文件已存在则覆盖
            }

            // 递归复制所有子目录
            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                if (subDir.EndsWith(excludeDir)) continue;

                string subDirName = Path.GetFileName(subDir);
                string destSubDir = Path.Combine(destDir, subDirName);
                CopyDirectory(subDir, destSubDir, excludeDir);
            }
        }
        private void ClearTask()
        {
            if (config.Data.Client.OnlyNode)
            {
                Task.Run(async () =>
                {
                    string path = Path.GetFullPath("./web");
                    while (true)
                    {
                        if (Directory.Exists(path))
                        {
                            DeleteDirectory(path);
                        }

                        await Task.Delay(1500);
                    }
                });
            }
        }
    }

    public sealed class ConfigInstallInfo
    {
        public ConfigInstallClientInfo Client { get; set; } = new ConfigInstallClientInfo();
        public ConfigInstallServerInfo Server { get; set; } = new ConfigInstallServerInfo();
        public ConfigInstallCommonInfo Common { get; set; } = new ConfigInstallCommonInfo();
    }
    public sealed class ConfigInstallClientInfo
    {
        public string Name { get; set; }
        public string GroupId { get; set; }
        public int Api { get; set; }
        public int Web { get; set; }
        public string Password { get; set; }

        public bool HasServer { get; set; }
        public string Server { get; set; }
        public string SForwardSecretKey { get; set; }
        public string RelaySecretKey { get; set; }
        public string UpdaterSecretKey { get; set; }
    }
    public sealed class ConfigInstallServerInfo
    {
        public int ServicePort { get; set; }
        public ConfigInstallServerRelayInfo Relay { get; set; }
        public ConfigInstallServerSForwardInfo SForward { get; set; }
        public ConfigInstallServerUpdaterInfo Updater { get; set; }
    }
    public sealed class ConfigInstallServerUpdaterInfo
    {
        public string SecretKey { get; set; }
    }
    public sealed class ConfigInstallServerRelayInfo
    {
        public string SecretKey { get; set; }
    }
    public sealed class ConfigInstallServerSForwardInfo
    {
        public string SecretKey { get; set; }
        public int WebPort { get; set; }
        public int[] TunnelPortRange { get; set; }
    }

    public sealed class ConfigInstallCommonInfo
    {
        public string[] Modes { get; set; }
    }
}
