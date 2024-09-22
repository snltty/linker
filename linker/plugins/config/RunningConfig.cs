using linker.store;
using linker.libs;
using LiteDB;
using System.Text.Json.Serialization;
using linker.config;
using linker.plugins.relay.transport;
using linker.libs.extends;

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
            LoggerHelper.Instance.Info($"config Sync:{fileConfig.GetHashCode()}");

            if (Data.IsSync5) return;

            LoggerHelper.Instance.Info($"config Sync1");

            if (Data.Client.Servers.Length > 0)
            {
                fileConfig.Data.Client.Servers = Data.Client.Servers;
                foreach (var server in Data.Client.Servers) server.Name = "Linker";
            }
            if (Data.Relay.Servers.Length > 0)
            {
                fileConfig.Data.Client.Relay.Servers = Data.Relay.Servers;
                foreach (var server in fileConfig.Data.Client.Relay.Servers) server.Name = "Linker";
            }

            fileConfig.Data.Client.SForward.SecretKey = Data.SForwardSecretKey;
            fileConfig.Data.Client.Updater.SecretKey = Data.UpdaterSecretKey;


            if (Data.Tunnel.Servers.Count > 0)
            {
                fileConfig.Data.Client.Tunnel.Servers = Data.Tunnel.Servers;
            }
            if (Data.Tunnel.Transports.Count > 0)
            {
                fileConfig.Data.Client.Tunnel.Transports = Data.Tunnel.Transports;
            }
            fileConfig.Save();

            LoggerHelper.Instance.Info($"config Sync2:{fileConfig.Data.Client.Servers.ToJson()}");

            Data.IsSync5 = true;
            Data.Update();
        }
    }

    public sealed partial class RunningConfigInfo
    {
        public ObjectId Id { get; set; }

        [JsonIgnore, BsonIgnore]
        public uint Updated { get; set; } = 1;

        public bool IsSync { get; set; }
        public bool IsSync2 { get; set; }
        public bool IsSync3 { get; set; }
        public bool IsSync4 { get; set; }
        public bool IsSync5 { get; set; }

        public void Update()
        {
            Updated++;
        }
    }

}
