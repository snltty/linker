using linker.libs.api;
using linker.libs.extends;
using System.IO.Compression;
using linker.libs;
using linker.messenger.signin;
using linker.messenger.api;
using System.Text;
using linker.messenger.relay.client.transport;
namespace linker.messenger.store.file
{
    public sealed class ConfigApiController : IApiController
    {
        private readonly RunningConfig runningConfig;
        private readonly FileConfig config;
        private readonly SignInClientTransfer signInClientTransfer;
        private readonly IMessengerSender sender;
        private readonly SignInClientState signInClientState;
        private readonly IApiStore apiStore;
        private readonly ExportResolver exportResolver;

        public ConfigApiController(RunningConfig runningConfig, FileConfig config, SignInClientTransfer signInClientTransfer, IMessengerSender sender, SignInClientState signInClientState, IApiStore apiStore, ExportResolver exportResolver)
        {
            this.runningConfig = runningConfig;
            this.config = config;
            this.signInClientTransfer = signInClientTransfer;
            this.sender = sender;
            this.signInClientState = signInClientState;
            this.apiStore = apiStore;
            this.exportResolver = exportResolver;
        }

        public object Get(ApiControllerParamsInfo param)
        {
            return new { Common = config.Data.Common, Client = config.Data.Client, Server = config.Data.Server, Running = runningConfig.Data };
        }
        public bool Install(ApiControllerParamsInfo param)
        {
            if (config.Data.Common.Install) return true;
            ConfigInstallInfo info = param.Content.DeJson<ConfigInstallInfo>();

            if (info.Common.Modes.Contains("client"))
            {
                config.Data.Client.Name = info.Client.Name;
                config.Data.Client.Groups = new SignInClientGroupInfo[] { new SignInClientGroupInfo { Id = info.Client.GroupId, Name = info.Client.GroupId, Password = info.Client.GroupPassword } };
                config.Data.Client.CApi.WebPort = info.Client.Web;
                config.Data.Client.CApi.ApiPort = info.Client.Api;
                config.Data.Client.CApi.ApiPassword = info.Client.Password;

                if (info.Client.HasServer)
                {
                    config.Data.Client.SForward.SecretKey = info.Client.SForwardSecretKey;
                    config.Data.Client.Updater.SecretKey = info.Client.UpdaterSecretKey;
                    foreach (var item in config.Data.Client.Relay.Servers)
                    {
                        item.SecretKey = info.Client.RelaySecretKey;
                    }
                    foreach (var item in config.Data.Client.Servers)
                    {
                        item.Host = info.Client.Server;
                        item.SecretKey = info.Client.ServerSecretKey;
                    }
                }
            }
            if (info.Common.Modes.Contains("server"))
            {
                config.Data.Server.ServicePort = info.Server.ServicePort;

                config.Data.Server.Relay.SecretKey = info.Server.Relay.SecretKey;

                config.Data.Server.SignIn.SecretKey = info.Server.SignIn.SecretKey;

                config.Data.Server.SForward.SecretKey = info.Server.SForward.SecretKey;
                config.Data.Server.SForward.WebPort = info.Server.SForward.WebPort;
                config.Data.Server.SForward.TunnelPortRange = info.Server.SForward.TunnelPortRange;

                config.Data.Server.Updater.SecretKey = info.Server.Updater.SecretKey;
            }

            config.Data.Common.Modes = info.Common.Modes;
            config.Data.Common.Install = true;
            config.Data.Update();

            return true;
        }

        public bool InstallCopy(ApiControllerParamsInfo param)
        {
            try
            {
                Dictionary<string, string> dic = Encoding.UTF8.GetString(Convert.FromBase64String(param.Content)).DeJson<Dictionary<string, string>>();
                config.Save(dic);
                return true;
            }
            catch (Exception)
            {
            }
            return false;
        }
        public async Task<bool> InstallSave(ApiControllerParamsInfo param)
        {
            try
            {
                InstallSaveInfo info = param.Content.DeJson<InstallSaveInfo>();

                string value = await exportResolver.Get(info.Server, info.Value);
                Dictionary<string, string> dic = Encoding.UTF8.GetString(Convert.FromBase64String(value)).DeJson<Dictionary<string, string>>();
                config.Save(dic);
                return true;
            }
            catch (Exception)
            {
            }
            return false;
        }

        [Access(AccessValue.Export)]
        public async Task<string> Copy(ApiControllerParamsInfo param)
        {
            try
            {
                ConfigExportInfo configExportInfo = param.Content.DeJson<ConfigExportInfo>();

                var (client, clientObject, common, commonObject) = await GetConfig(configExportInfo).ConfigureAwait(false);
                Dictionary<string, string> dic = new Dictionary<string, string>
                {
                    {"Client",clientObject.ToJson()},
                    {"Common",commonObject.ToJson()},
                };

                return Convert.ToBase64String(Encoding.UTF8.GetBytes(dic.ToJson()));
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
            }
            return string.Empty;
        }
        [Access(AccessValue.Export)]
        public async Task<string> Save(ApiControllerParamsInfo param)
        {
            try
            {
                ConfigExportInfo configExportInfo = param.Content.DeJson<ConfigExportInfo>();

                var (client, clientObject, common, commonObject) = await GetConfig(configExportInfo).ConfigureAwait(false);
                Dictionary<string, object> dic = new Dictionary<string, object>
                {
                    {"Client",clientObject},
                    {"Common",commonObject},
                };
                string value = Convert.ToBase64String(Encoding.UTF8.GetBytes(dic.ToJson()));
                return await exportResolver.Save(signInClientState.Connection.Address, value);
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
            }
            return string.Empty;
        }
        [Access(AccessValue.Export)]
        public async Task<bool> Export(ApiControllerParamsInfo param)
        {
            try
            {
                ConfigExportInfo configExportInfo = param.Content.DeJson<ConfigExportInfo>();

                string dirName = $"client-node-export";
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

                string configPath = Path.Combine(rootPath, $"configs");
                Directory.CreateDirectory(configPath);

                var (client, clientObject, common, commonObject) = await GetConfig(configExportInfo).ConfigureAwait(false);
                File.WriteAllText(Path.Combine(configPath, $"client.json"), config.Data.Client.Serialize(clientObject));
                File.WriteAllText(Path.Combine(configPath, $"common.json"), config.Data.Common.Serialize(commonObject));


                ZipFile.CreateFromDirectory(rootPath, zipPath);
                DeleteDirectory(rootPath);

                return string.IsNullOrWhiteSpace(client.Id) == false;
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
            }
            return false;
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


        private async Task<(ConfigClientInfo, object, ConfigCommonInfo, object)> GetConfig(ConfigExportInfo configExportInfo)
        {
            ConfigClientInfo client = config.Data.Client.ToJson().DeJson<ConfigClientInfo>();
            client.Id = string.Empty;
            client.Name = string.Empty;
            if (configExportInfo.Single || client.OnlyNode)
            {
                client.Id = await signInClientTransfer.GetNewId().ConfigureAwait(false);
                client.Name = configExportInfo.Name;
            }
            if (client.OnlyNode == false)
            {
                client.CApi.ApiPassword = configExportInfo.ApiPassword;
            }

            client.Access = (AccessValue)((ulong)config.Data.Client.Access & configExportInfo.Access);


            if (configExportInfo.Relay) client.Relay = new RelayClientInfo { Servers = new RelayServerInfo[] { client.Relay.Servers[0] } };
            else client.Relay = new RelayClientInfo { Servers = new RelayServerInfo[] { new RelayServerInfo { } } };

            if (configExportInfo.SForward) client.SForward = new linker.messenger.sforward.SForwardConfigClientInfo { SecretKey = client.SForward.SecretKey };
            else client.SForward = new linker.messenger.sforward.SForwardConfigClientInfo { };

            if (configExportInfo.Server) client.Servers = new SignInClientServerInfo[] { client.Servers[0] };
            else client.Servers = new SignInClientServerInfo[] { };

            if (configExportInfo.Group) client.Groups = new SignInClientGroupInfo[] { client.Groups[0] };
            else client.Groups = new SignInClientGroupInfo[] { };

            if (configExportInfo.Updater) client.Updater = new linker.messenger.updater.UpdaterConfigClientInfo { SecretKey = client.Updater.SecretKey };
            else client.Updater = new linker.messenger.updater.UpdaterConfigClientInfo { };


            if (configExportInfo.Tunnel) client.Tunnel = new TunnelConfigClientInfo { Transports = client.Tunnel.Transports };
            else client.Tunnel = new TunnelConfigClientInfo { Transports = new List<linker.tunnel.transport.TunnelTransportItemInfo>() };

            ConfigCommonInfo common = config.Data.Common.ToJson().DeJson<ConfigCommonInfo>();
            common.Install = true;
            common.Modes = ["client"];

            return (client, new
            {
                client.Id,
                client.Name,
                client.CApi,
                client.Access,
                Groups = new SignInClientGroupInfo[] { config.Data.Client.Groups[0] },
                Servers = new SignInClientServerInfo[] { config.Data.Client.Servers[0] },
                client.SForward,
                client.Updater,
                Relay = new { Servers = new RelayServerInfo[] { client.Relay.Servers[0] } },
                client.Tunnel,
            }, common, new { Install = true, Modes = new string[] { "client" } });
        }

    }

    public sealed class InstallSaveInfo
    {
        public string Server { get; set; }
        public string Value { get; set; }
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
        public string GroupPassword { get; set; }

        public int Api { get; set; }
        public int Web { get; set; }
        public string Password { get; set; }

        public bool HasServer { get; set; }
        public string Server { get; set; }
        public string ServerSecretKey { get; set; }

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
        public ConfigInstallServerSignInfo SignIn { get; set; }
    }
    public sealed class ConfigInstallServerSignInfo
    {
        public string SecretKey { get; set; }
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

    public sealed class ConfigExportInfo
    {
        public string Name { get; set; }
        public string ApiPassword { get; set; }
        public bool Single { get; set; }
        public ulong Access { get; set; }

        public bool Relay { get; set; }
        public bool SForward { get; set; }
        public bool Updater { get; set; }
        public bool Server { get; set; }
        public bool Group { get; set; }
        public bool Tunnel { get; set; }

    }

}
