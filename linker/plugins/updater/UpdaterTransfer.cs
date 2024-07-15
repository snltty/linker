using linker.config;
using linker.libs;
using System.Diagnostics;

namespace linker.plugins.updater
{
    public sealed class UpdaterTransfer
    {
        private UpdateInfo updateInfo;
        private string rootPath = "./updater";

        private readonly FileConfig fileConfig;
        public UpdaterTransfer(FileConfig fileConfig)
        {
            this.fileConfig = fileConfig;
            RestartUpdater();
            LoadUpdater();
        }

        private void RestartUpdater()
        {
            foreach (var item in Process.GetProcessesByName("linker.updater"))
            {
                item.Kill();
            }
            CommandHelper.Execute("linker.updater.exe", rootPath);
        }
        private void LoadUpdater()
        {
            try
            {
                updateInfo = new UpdateInfo
                {
                    Msg = File.ReadAllText(Path.Join(rootPath, "msg.txt")),
                    Version = File.ReadAllText(Path.Join(rootPath, "version.txt"))
                };
            }
            catch (Exception)
            {
            }
        }

        public UpdateInfo Get()
        {
            LoadUpdater();
            return updateInfo;
        }
        public void Update()
        {
            if (updateInfo == null || string.IsNullOrWhiteSpace(updateInfo.Version))
            {
                return;
            }
            File.WriteAllText(Path.Join(rootPath, "extract.txt"), $"{fileConfig.Data.Client.Updater.RunCommand}{Environment.NewLine}{fileConfig.Data.Client.Updater.StopCommand}");
        }
    }

    public sealed class UpdateInfo
    {
        public string Version { get; set; }
        public string Msg { get; set; }
    }
}
