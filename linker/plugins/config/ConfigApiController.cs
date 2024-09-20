using linker.config;
using linker.libs.api;
using linker.libs.extends;
using linker.client.config;
using linker.plugins.capi;
using System.IO.Compression;
using linker.libs;
using linker.plugins.client;
using linker.plugins.messenger;
using linker.plugins.config.messenger;
using MemoryPack;

namespace linker.plugins.config
{
    public sealed class ConfigClientApiController : IApiClientController
    {
        private readonly RunningConfig runningConfig;
        private readonly FileConfig config;
        private readonly ClientSignInTransfer clientSignInTransfer;
        private readonly MessengerSender sender;
        private readonly ClientSignInState clientSignInState;
        private readonly AccessTransfer accessTransfer;

        public ConfigClientApiController(RunningConfig runningConfig, FileConfig config, ClientSignInTransfer clientSignInTransfer, MessengerSender sender, ClientSignInState clientSignInState, AccessTransfer accessTransfer)
        {
            this.runningConfig = runningConfig;
            this.config = config;
            this.clientSignInTransfer = clientSignInTransfer;
            this.sender = sender;
            this.clientSignInState = clientSignInState;
            this.accessTransfer = accessTransfer;
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
                    runningConfig.Data.SForwardSecretKey = info.Client.SForwardSecretKey;
                    runningConfig.Data.UpdaterSecretKey = info.Client.UpdaterSecretKey;
                    foreach (var item in runningConfig.Data.Relay.Servers)
                    {
                        item.SecretKey = info.Client.RelaySecretKey;
                        item.Host = info.Client.Server;
                    }
                    foreach (var item in runningConfig.Data.Tunnel.Servers)
                    {
                        item.Host = info.Client.Server;
                    }
                    foreach (var item in runningConfig.Data.Client.Servers)
                    {
                        item.Host = info.Client.Server;
                        item.SecretKey = info.Client.ServerSecretKey;
                    }
                    if (runningConfig.Data.Client.Servers.Length > 0)
                        config.Data.Client.ServerInfo = runningConfig.Data.Client.Servers.FirstOrDefault();
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

            runningConfig.Data.Update();

            return true;
        }


        public AccessListInfo GetAccesss(ApiControllerParamsInfo param)
        {
            ulong hashCode = ulong.Parse(param.Content);
            if (accessTransfer.Version.Eq(hashCode, out ulong version) == false)
            {
                return new AccessListInfo
                {

                    HashCode = version,
                    List = accessTransfer.GetAccesss()

                };
            }
            return new AccessListInfo { HashCode = version };
        }


        [ClientApiAccessAttribute(ClientApiAccess.Access)]
        public async Task<bool> SetAccess(ApiControllerParamsInfo param)
        {
            ConfigUpdateAccessInfo configUpdateAccessInfo = param.Content.DeJson<ConfigUpdateAccessInfo>();
            if (configUpdateAccessInfo.ToMachineId == config.Data.Client.Id)
            {
                return false;
            }
            configUpdateAccessInfo.FromMachineId = config.Data.Client.Id;
            MessageResponeInfo resp = await sender.SendReply(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)ConfigMessengerIds.AccessUpdateForward,
                Payload = MemoryPackSerializer.Serialize(configUpdateAccessInfo)
            });
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }

        [ClientApiAccessAttribute(ClientApiAccess.Export)]
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

                ConfigClientInfo client = config.Data.Client.ToJson().DeJson<ConfigClientInfo>();
                if (configExportInfo.Single || client.OnlyNode)
                {
                    client.Id = await clientSignInTransfer.GetNewId();
                }
                if (client.OnlyNode == false)
                {
                    client.CApi.ApiPassword = configExportInfo.ApiPassword;
                }
                client.Name = configExportInfo.Name;
                client.Access = accessTransfer.AssignAccess((ClientApiAccess)configExportInfo.Access);
                client.OnlyNode = true;
                client.NodeArg = configExportInfo.ActionArg;
                File.WriteAllText(Path.Combine(configPath, $"client.json"), client.Serialize(client));

                ConfigCommonInfo common = config.Data.Common.ToJson().DeJson<ConfigCommonInfo>();
                common.Install = true;
                common.Modes = ["client"];
                File.WriteAllText(Path.Combine(configPath, $"common.json"), common.ToJsonFormat());


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
        public string ActionArg { get; set; }
        public bool Single { get; set; }
        public ulong Access { get; set; }
    }

    public sealed class AccessListInfo
    {
        public Dictionary<string, ClientApiAccess> List { get; set; }
        public ulong HashCode { get; set; }
    }
}
