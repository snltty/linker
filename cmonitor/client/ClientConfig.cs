using cmonitor.client.reports.hijack;
using cmonitor.client.reports.screen;
using common.libs.database;
using System.ComponentModel.DataAnnotations.Schema;

namespace cmonitor.client
{

    [Table("client")]
    public sealed class ClientConfig
    {
        private readonly IConfigDataProvider<ClientConfig> configDataProvider;
        private readonly Config config;

        public ClientConfig() { }
        public ClientConfig(IConfigDataProvider<ClientConfig> configDataProvider, Config config)
        {
            this.configDataProvider = configDataProvider;
            this.config = config;

            ClientConfig clientConfig = configDataProvider.Load().Result ?? new ClientConfig();
            LLock = clientConfig.LLock;
            Wallpaper = clientConfig.Wallpaper;
            WallpaperUrl = clientConfig.WallpaperUrl;
            HijackConfig = clientConfig.HijackConfig;
            HijackIds = clientConfig.HijackIds;
            WindowNames = clientConfig.WindowNames;
            WindowIds = clientConfig.WindowIds;
            UserSid = clientConfig.UserSid;
            Wlan = clientConfig.Wlan;
            WlanAuto = clientConfig.WlanAuto;
            SaveTask();
        }

        private void SaveTask()
        {
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    try
                    {
                        if (updated && config.SaveSetting)
                        {
                            Save();
                        }
                    }
                    catch (Exception)
                    {
                    }
                    await Task.Delay(5000);
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

        private uint[] _hijackIds = Array.Empty<uint>();
        public uint[] HijackIds
        {
            get => _hijackIds; set
            {
                _hijackIds = value;
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

        private uint[] _windowIds = Array.Empty<uint>();
        public uint[] WindowIds
        {
            get => _windowIds; set
            {
                _windowIds = value;
                updated = true;
            }
        }

       

        private string _userSid = string.Empty;
        public string UserSid
        {
            get => _userSid; set
            {
                _userSid = value;
                updated = true;
            }
        }


        private string _wlan = string.Empty;
        public string Wlan
        {
            get => _wlan; set
            {
                _wlan = value;
                updated = true;
            }
        }
        private bool _wlanAuto;
        public bool WlanAuto
        {
            get => _wlanAuto; set
            {
                _wlanAuto = value;
                updated = true;
            }
        }

        public void Save()
        {
            configDataProvider.Save(this).Wait();
        }
    }
}
