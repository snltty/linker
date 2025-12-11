using linker.libs.extends;
using System.IO.Compression;
using linker.libs;
using linker.messenger.signin;
using linker.messenger.api;
using System.Text;
using System.Text.Json;
using System.Collections;
using linker.libs.web;
namespace linker.messenger.store.file
{
    public sealed class ConfigApiController : IApiController
    {
        private readonly RunningConfig runningConfig;
        private readonly FileConfig config;
        private readonly SignInClientTransfer signInClientTransfer;
        private readonly SignInClientState signInClientState;
        private readonly ExportResolver exportResolver;
        private readonly IAccessStore accessStore;

        public ConfigApiController(RunningConfig runningConfig, FileConfig config, SignInClientTransfer signInClientTransfer,
            SignInClientState signInClientState, ExportResolver exportResolver, IAccessStore accessStore)
        {
            this.runningConfig = runningConfig;
            this.config = config;
            this.signInClientTransfer = signInClientTransfer;
            this.signInClientState = signInClientState;
            this.exportResolver = exportResolver;
            this.accessStore = accessStore;
        }

        public ConfigListInfo Get(ApiControllerParamsInfo param)
        {
            GetConfigParamsInfo info = param.Content.DeJson<GetConfigParamsInfo>();

            bool eq = config.Data.DataVersion.Eq(info.HashCode, out ulong version),
                eq1 = runningConfig.Data.DataVersion.Eq(info.HashCode1, out ulong version1);
            return new ConfigListInfo
            {
                HashCode = version,
                HashCode1 = version1,
                List = new Dictionary<string, object>
                {
                    { "Common" ,eq ? null : config.Data.Common },
                    { "Client" ,eq ? null : config.Data.Client },
                    { "Server" ,eq ? null : config.Data.Server },
                    { "Running" ,eq1 ? null : runningConfig.Data}
                }
            };
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
                config.Data.Client.CApi.ApiPassword = info.Client.Password;

                if (info.Client.HasServer)
                {
                    foreach (var item in config.Data.Client.Servers)
                    {
                        item.Host = info.Client.Server;
                        item.Host1 = info.Client.Server1;
                        item.SuperKey = info.Client.SuperKey;
                        item.SuperPassword = info.Client.SuperPassword;
                    }
                }
            }
            if (info.Common.Modes.Contains("server"))
            {
                config.Data.Server.ServicePort = info.Server.ServicePort;
                config.Data.Server.SignIn.Anonymous = info.Server.Anonymous;
                config.Data.Server.SignIn.SuperKey = info.Server.SuperKey;
                config.Data.Server.SignIn.SuperPassword = info.Server.SuperPassword;
                //config.Data.Server.SForward.WebPort = info.Server.SForward.WebPort;
                //config.Data.Server.SForward.TunnelPorts = info.Server.SForward.TunnelPorts;
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
                using JsonDocument json = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(param.Content)));
                config.Save(json);
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
                using JsonDocument json = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(value)));
                config.Save(json);
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
                object dic = new
                {
                    Client = clientObject,
                    Common = commonObject
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
                object dic = new
                {
                    Client = clientObject,
                    Common = commonObject
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
            client.CApi.WebPort = configExportInfo.WebPort;

            client.AccessBits = accessStore.AssignAccess(configExportInfo.Access);
            client.FullAccess = configExportInfo.FullAccess && config.Data.Client.FullAccess;

            if (configExportInfo.Server)
            {
                client.Server.Host = config.Data.Client.Server.Host;
                client.Server.Host1 = config.Data.Client.Server.Host1;
                client.Server.Name = config.Data.Client.Server.Name;
                client.Server.UserId = config.Data.Client.Server.UserId;

                if (configExportInfo.Super)
                {
                    client.Server.SuperKey = config.Data.Client.Server.SuperKey;
                    client.Server.SuperPassword = config.Data.Client.Server.SuperPassword;
                }
            }
            else client.Servers = [];

            if (configExportInfo.Group) client.Groups = [client.Groups[0]];
            else client.Groups = [];

            if (configExportInfo.Updater) client.Updater = new linker.messenger.updater.UpdaterConfigClientInfo { Sync2Server = client.Updater.Sync2Server };
            else client.Updater = new linker.messenger.updater.UpdaterConfigClientInfo { };

            ConfigCommonInfo common = config.Data.Common.ToJson().DeJson<ConfigCommonInfo>();
            common.Install = true;
            common.Modes = ["client"];

            return (client, new
            {
                client.Id,
                client.Name,
                client.CApi,
                client.AccessBits,
                client.FullAccess,
                Groups = new SignInClientGroupInfo[] { client.Group },
                Servers = new SignInClientServerInfo[] { client.Server },
                client.Updater,
            }, common, new { Install = true, Modes = new string[] { "client" } });
        }

        [Access(AccessValue.Export)]
        public string ShareGroup(ApiControllerParamsInfo param)
        {
            ICrypto crypto = CryptoFactory.CreateSymmetric(Helper.GlobalString);

            try
            {
                return Convert.ToBase64String(crypto.Encode(new ShareGroupInfo
                {
                    Server = config.Data.Client.Server.Host,
                    Id = config.Data.Client.Group.Id,
                    Pwd = config.Data.Client.Group.Password
                }.ToJson().ToBytes()));
            }
            catch (Exception)
            {
            }
            finally
            {
                crypto.Dispose();
            }
            return string.Empty;
        }
        public bool JoinGroup(ApiControllerParamsInfo param)
        {
            ICrypto crypto = CryptoFactory.CreateSymmetric(Helper.GlobalString);

            try
            {
                ShareGroupInfo info = crypto.Decode(Convert.FromBase64String(param.Content)).GetString().DeJson<ShareGroupInfo>();
                config.Data.Client.Server.Host = info.Server;
                config.Data.Client.Group.Id = info.Id;
                config.Data.Client.Group.Password = info.Pwd;
                config.Data.Update();

                signInClientTransfer.ReSignIn();
                return true;
            }
            catch (Exception)
            {
            }
            finally
            {
                crypto.Dispose(); 
            }

            return false;
        }
    }

    public sealed class ShareGroupInfo
    {
        public string Server { get; set; }
        public string Id { get; set; }
        public string Pwd { get; set; }
    }

    public sealed class GetConfigParamsInfo
    {
        public ulong HashCode { get; set; }
        public ulong HashCode1 { get; set; }
    }

    public sealed class ConfigListInfo
    {
        public Dictionary<string, object> List { get; set; }
        public ulong HashCode { get; set; }
        public ulong HashCode1 { get; set; }
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

        public int Web { get; set; }
        public string Password { get; set; }

        public bool HasServer { get; set; }
        public string Server { get; set; }
        public string Server1 { get; set; }
        public string SuperKey { get; set; }
        public string SuperPassword { get; set; }
    }
    public sealed class ConfigInstallServerInfo
    {
        public int ServicePort { get; set; }
        public bool Anonymous { get; set; }
        public string SuperKey { get; set; }
        public string SuperPassword { get; set; }
        public ConfigInstallServerSForwardInfo SForward { get; set; }
    }
    public sealed class ConfigInstallServerSignInfo
    {
        public string SuperKey { get; set; }
        public string SuperPassword { get; set; }
    }
    public sealed class ConfigInstallServerSForwardInfo
    {
        public int WebPort { get; set; }
        public string TunnelPorts { get; set; }
    }

    public sealed class ConfigInstallCommonInfo
    {
        public string[] Modes { get; set; }
    }

    public sealed class ConfigExportInfo
    {
        public string Name { get; set; }
        public string ApiPassword { get; set; }
        public int WebPort { get; set; }
        public bool Single { get; set; }
        public BitArray Access { get; set; }
        public bool FullAccess { get; set; }

        public bool Relay { get; set; }
        public bool Updater { get; set; }
        public bool Server { get; set; }
        public bool Super { get; set; }
        public bool Group { get; set; }
        public bool Tunnel { get; set; }

    }

}
