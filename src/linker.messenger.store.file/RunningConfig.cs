using linker.libs;
using linker.libs.extends;
using linker.libs.timer;
using LiteDB;
using System.Text.Json.Serialization;

namespace linker.messenger.store.file
{
    /// <summary>
    /// 运行时配置
    /// </summary>
    public sealed class RunningConfig
    {
        private ILiteCollection<RunningConfigInfo> liteCollection;


        private SemaphoreSlim slim = new SemaphoreSlim(1);
        public RunningConfigInfo Data { get; private set; } = new RunningConfigInfo();

        private readonly Storefactory dBfactory;
        public RunningConfig(Storefactory dBfactory)
        {
            this.dBfactory = dBfactory;

            liteCollection = dBfactory.GetCollection<RunningConfigInfo>("running");

            Load();
            Save();
            SaveTask();

            Helper.OnAppExit += Helper_OnAppExit;
        }

        private void Helper_OnAppExit(object sender, EventArgs e)
        {
            dBfactory.Dispose();
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
            Data.Update();
            TimerHelper.SetIntervalLong(() =>
            {
                while (Data.Updated > 0)
                {
                    Save();
                    Data.Updated--;
                }
            }, 3000);
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
                
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
            }
            finally
            {
                GC.Collect();
                slim.Release();
            }
        }

    }

    public sealed partial class RunningConfigInfo
    {
        public ObjectId Id { get; set; }

        [JsonIgnore, BsonIgnore]
        public uint Updated { get; set; } = 1;
        [JsonIgnore, BsonIgnore]
        public VersionManager DataVersion { get; set; } = new VersionManager();

        public void Update()
        {
            Updated++;
            DataVersion.Increment();
        }
    }
}
