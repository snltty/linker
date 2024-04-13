using common.libs;
using common.libs.extends;
using System.Net;
using System.Text.Json.Serialization;

namespace cmonitor.client.running
{
    public sealed class RunningConfig
    {
        private FileStream fs = null;
        private StreamWriter writer = null;
        private StreamReader reader = null;
        private SemaphoreSlim slim = new SemaphoreSlim(1);
        private string configPath { get; } = "./configs/";

        public RunningConfigInfo Data { get; private set; } = new RunningConfigInfo();


        public RunningConfig()
        {
            if (Directory.Exists(configPath) == false)
            {
                Directory.CreateDirectory(configPath);
            }
            string path = Path.Join(configPath, "running.json");
            fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            reader = new StreamReader(fs, System.Text.Encoding.UTF8);
            writer = new StreamWriter(fs, System.Text.Encoding.UTF8);

            Load();
            Save();
            SaveTask();
        }

        private void Load()
        {
            slim.Wait();
            try
            {
                fs.Seek(0, SeekOrigin.Begin);
                string text = reader.ReadToEnd();
                if (string.IsNullOrWhiteSpace(text))
                {
                    return;
                }
                Data = text.DeJson<RunningConfigInfo>();
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
                    uint updated = Data.Updated;
                    while (updated > 0)
                    {
                        Save();
                        updated--;
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
                fs.Seek(0, SeekOrigin.Begin);
                fs.SetLength(0);
                writer.Write(Data.ToJsonFormat());
                writer.Flush();
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
        [JsonIgnore]
        public uint Updated { get; set; } = 1;

        public void Update()
        {
            Updated++;
        }
    }
}
