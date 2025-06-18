using Microsoft.Win32;
namespace linker.messenger.rapp
{
    public sealed class RemoteAppTransfer
    {
        public List<RemoteAppInfo> Get()
        {
            List<RemoteAppInfo> result = new List<RemoteAppInfo>();

            using RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Terminal Server\\TSAppAllowList\\Applications");
            if (registryKey == null)
            {
                return result;
            }
            foreach (string appName in registryKey.GetSubKeyNames())
            {
                using RegistryKey app = registryKey.OpenSubKey(appName);
                result.Add(new RemoteAppInfo
                {
                    Name = app.GetValue("Name")?.ToString() ?? appName,
                    Path = app.GetValue("Path")?.ToString() ?? string.Empty,
                    IconPath = app.GetValue("IconPath")?.ToString() ?? string.Empty,
                    IconIndex = (byte)(app.GetValue("IconIndex") ?? 0),
                    CommandLineSetting = (byte)(app.GetValue("CommandLineSetting") ?? 1),
                    ShowInTSWA = (byte)(app.GetValue("ShowInTSWA") ?? 1),
                    VPath = app.GetValue("VPath")?.ToString() ?? string.Empty
                });
            }
            return result;
        }
    }
}
