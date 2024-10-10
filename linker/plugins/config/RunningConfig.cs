using linker.store;
using linker.libs;
using LiteDB;
using System.Text.Json.Serialization;
using linker.config;
using MemoryPack;
using linker.libs.extends;
using linker.tunnel.wanport;

namespace linker.client.config
{
    /// <summary>
    /// 运行时配置
    /// </summary>
    public sealed class RunningConfig
    {
        private readonly ILiteCollection<RunningConfigInfo> liteCollection;


        private SemaphoreSlim slim = new SemaphoreSlim(1);
        public RunningConfigInfo Data { get; private set; } = new RunningConfigInfo();

        private readonly Storefactory dBfactory;
        private readonly FileConfig fileConfig;
        public RunningConfig(Storefactory dBfactory, FileConfig fileConfig)
        {
            this.dBfactory = dBfactory;
            this.fileConfig = fileConfig;

            liteCollection = dBfactory.GetCollection<RunningConfigInfo>("running");

            Load();
            Sync();
            Save();
            SaveTask();
        }

        private void Load()
        {
            slim.Wait();
            try
            {
                Data = liteCollection.FindAll().FirstOrDefault() ?? new RunningConfigInfo();
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
            }
            finally
            {
                slim.Release();
            }
        }
        private void SaveTask()
        {
            TimerHelper.SetInterval(() =>
            {
                while (Data.Updated > 0)
                {
                    Save();
                    Data.Updated--;
                }
                return true;
            }, 1000);
        }
        private void Save()
        {
            slim.Wait();
            try
            {
                RunningConfigInfo old = liteCollection.FindAll().FirstOrDefault();
                if (old == null)
                {
                    liteCollection.Insert(Data);
                }
                else
                {
                    Type type = Data.GetType();
                    foreach (var property in old.GetType().GetProperties().Where(c => c.Name != "Id"))
                    {
                        try
                        {
                            property.SetValue(old, type.GetProperty(property.Name).GetValue(Data));
                        }
                        catch (Exception)
                        {
                        }
                    };
                    liteCollection.Update(old.Id, old);
                }
                dBfactory.Confirm();
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
            }
            finally
            {
                slim.Release();
            }
        }

        private void Sync()
        {
            if (Data.IsSyncConfig) return;

            bool updated = false;
            if (Data.Client.Servers.Length > 0)
            {
                fileConfig.Data.Client.Servers = Data.Client.Servers;
                foreach (var server in Data.Client.Servers) server.Name = "Linker";
                updated = true;
            }
            if (Data.Relay.Servers.Length > 0)
            {
                fileConfig.Data.Client.Relay.Servers = Data.Relay.Servers;
                foreach (var server in fileConfig.Data.Client.Relay.Servers) server.Name = "Linker";
                updated = true;

                fileConfig.Data.Client.SForward.SecretKey = Data.SForwardSecretKey;
                fileConfig.Data.Client.Updater.SecretKey = Data.UpdaterSecretKey;
            }

            if (Data.Tunnel.Servers.Count > 0)
            {
                fileConfig.Data.Client.Tunnel.Servers = Data.Tunnel.Servers;
                updated = true;
            }
            if (Data.Tunnel.Transports.Count > 0)
            {
                fileConfig.Data.Client.Tunnel.Transports = Data.Tunnel.Transports;
                updated = true;
            }

            Data.IsSyncConfig = true;
            Save();

            if (updated)
            {
                fileConfig.Save();
                LoggerHelper.Instance.Warning($"sync config，restarting");
                Environment.Exit(1);
            }

        }
    }

    public sealed partial class RunningConfigInfo
    {
        public RunningConfigInfo() { }

        public ObjectId Id { get; set; }

        [JsonIgnore, BsonIgnore]
        public uint Updated { get; set; } = 1;

        public bool IsSyncConfig { get; set; }

        public void Update()
        {
            Updated++;
        }
    }


    [MemoryPackable]
    public sealed partial class SecretKeyAsyncInfo
    {
        public SecretKeyAsyncInfo() { }
        public string SignSecretKey {  get; set; }
        public string RelaySecretKey {  get; set; }
        public string SForwardSecretKey {  get; set; }
    }

    [MemoryPackable]
    public sealed partial class ServerAsyncInfo
    {
        public ServerAsyncInfo() { }
        public ClientServerInfo[] SignServers { get; set; }
        public RelayServerInfo[] RelayServers { get; set; }
        public TunnelWanPortInfo[] TunnelServers { get; set; }
    }
}
