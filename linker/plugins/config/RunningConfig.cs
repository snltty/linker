using linker.store;
using linker.libs;
using LiteDB;
using System.Text.Json.Serialization;

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
        public RunningConfig(Storefactory dBfactory)
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
                LoggerHelper.Instance.Error(ex);
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
                    await Task.Delay(1000).ConfigureAwait(false);
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
                        try
                        {
                            property.SetValue(old, type.GetProperty(property.Name).GetValue(Data));
                        }
                        catch (Exception)
                        {
                        }
                    };
                    liteCollection.Update(old.Id,old);
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
