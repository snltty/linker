using cmonitor.hijack;
using common.libs.database;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace cmonitor.server.client
{

    [Table("client")]
    public sealed class ClientConfig
    {
        private readonly IConfigDataProvider<ClientConfig> configDataProvider;
        public ClientConfig() { }
        public ClientConfig(IConfigDataProvider<ClientConfig> configDataProvider)
        {
            this.configDataProvider = configDataProvider;
            ClientConfig config = configDataProvider.Load().Result ?? new ClientConfig();
            LLock = config.LLock;
            Wallpaper = config.Wallpaper;
            WallpaperUrl = config.WallpaperUrl;
            HijackConfig = config.HijackConfig;
            WindowNames = config.WindowNames;
            SaveTask();
        }

        private void SaveTask()
        {
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    try
                    {
                        if (updated)
                        {
                            Save();
                        }
                    }
                    catch (Exception)
                    {
                    }
                    System.Threading.Thread.Sleep(5000);
                }
            }, TaskCreationOptions.LongRunning);
        }

        private bool updated = false;

        private bool _llock;
        public bool LLock
        {
            get => _llock; set
            {
                _llock = value;
                updated = true;
            }
        }

        private bool _wallpaper;
        public bool Wallpaper
        {
            get => _wallpaper; set
            {
                _wallpaper = value;
                updated = true;
            }
        }

        private string _wallpaperUrl;
        public string WallpaperUrl
        {
            get => _wallpaperUrl; set
            {
                _wallpaperUrl = value;
                updated = true;
            }
        }

        private HijackConfig _hijackConfig = new HijackConfig();
        public HijackConfig HijackConfig
        {
            get => _hijackConfig; set
            {
                _hijackConfig = value;
                updated = true;
            }
        }

        private string[] _windowNames = Array.Empty<string>();
        public string[] WindowNames
        {
            get => _windowNames; set
            {
                _windowNames = value;
                updated = true;
            }
        }


        public void Save()
        {
            configDataProvider.Save(this).Wait();
        }
    }



}
