using cmonitor.db;
using common.libs;
using LiteDB;
using System.Text.Json.Serialization;

namespace cmonitor.client.config
{
    public sealed class RunningConfig
    {
        private readonly ILiteCollection<RunningConfigInfo> liteCollection;


        private SemaphoreSlim slim = new SemaphoreSlim(1);
        public RunningConfigInfo Data { get; private set; } = new RunningConfigInfo();

        private readonly DBfactory dBfactory;
        public RunningConfig(DBfactory dBfactory)
        {
            this.dBfactory = dBfactory;
            liteCollection = dBfactory.GetCollection<RunningConfigInfo>("running");

            Load();
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
                Logger.Instance.Error(ex);
            }
            finally
            {
                slim.Release();
            }
        }
        private void SaveTask()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    while (Data.Updated > 0)
                    {
                        Save();
                        Data.Updated--;
                    }
                    await Task.Delay(1000);
                }
            });
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
                        property.SetValue(old, type.GetProperty(property.Name).GetValue(Data));
                    };
                    liteCollection.Update(old.Id,old);
                }
                dBfactory.Confirm();
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
            finally
            {
                slim.Release();
            }
        }
    }

    public sealed partial class RunningConfigInfo
    {
        public ObjectId Id { get; set; }

        [JsonIgnore, BsonIgnore]
        public uint Updated { get; set; } = 1;

        public void Update()
        {
            Updated++;
        }
    }

}
