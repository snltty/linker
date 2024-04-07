using ManagedNativeWifi;
using System.Net.NetworkInformation;

namespace cmonitor.plugins.wlan.report
{
    public class WlanWindows : IWlan
    {
        public List<string> WlanEnums()
        {
            return NativeWifi.EnumerateAvailableNetworks().Where(c => string.IsNullOrWhiteSpace(c.ProfileName) == false).Select(c => c.ProfileName).ToList();
        }

        public async Task<bool> WlanConnect(string name)
        {
            var wifi = NativeWifi.EnumerateAvailableNetworks().FirstOrDefault(c => c.ProfileName == name);
            if (wifi == null)
            {
                return false;
            }
            return await NativeWifi.ConnectNetworkAsync(wifi.Interface.Id, wifi.ProfileName, wifi.BssType, TimeSpan.FromSeconds(5));
        }

        public bool Connected()
        {
            //using Ping ping = new Ping();
            ////var replay = ping.Send("www.baidu.com", 5000);
            //return replay.Status == IPStatus.Success;
            return common.libs.winapis.Wininet.InternetGetConnectedState(out int desc, 0);
        }
    }
}
