using ManagedNativeWifi;

namespace cmonitor.plugins.wlan.report
{
    public class WlanWindows : IWlan
    {
        private Guid id;
        public void Init()
        {
            try
            {
                var interfaces = NativeWifi.EnumerateAvailableNetworks().FirstOrDefault(c => string.IsNullOrWhiteSpace(c.ProfileName) == false);
                if (interfaces != null)
                {
                    id = interfaces.Interface.Id;
                }
            }
            catch (Exception)
            {
            }
        }
        public List<string> Enums()
        {
            return NativeWifi.EnumerateAvailableNetworks().Where(c => string.IsNullOrWhiteSpace(c.ProfileName) == false).Select(c => c.ProfileName).ToList();
        }

        public async Task<bool> Connect()
        {
            if (Connected() == false)
            {
                try
                {
                    NativeWifi.TurnOnInterfaceRadio(id);
                }
                catch (Exception)
                {
                }
                foreach (string wifi in Enums())
                {
                    if (await Connect(wifi))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private async Task<bool> Connect(string name)
        {
            var wifi = NativeWifi.EnumerateAvailableNetworks().FirstOrDefault(c => c.ProfileName == name);
            if (wifi == null)
            {
                return false;
            }

            id = wifi.Interface.Id;
            return await NativeWifi.ConnectNetworkAsync(wifi.Interface.Id, wifi.ProfileName, wifi.BssType, TimeSpan.FromSeconds(5));
        }
        private bool Connected()
        {
            return common.libs.winapis.Wininet.InternetGetConnectedState(out int desc, 0);
        }


    }
}
