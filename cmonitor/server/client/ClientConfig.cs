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
            Lock = config.Lock;
            Wallpaper = config.Wallpaper;
            Usb = config.Usb;
            Save();
        }

        public bool Lock { get; set; }
        public bool Wallpaper { get; set; }
        public bool Usb { get; set; }

        public void Save()
        {
            configDataProvider.Save(this).Wait();
        }
    }



}
