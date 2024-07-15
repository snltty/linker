using linker.plugins.updater.config;

namespace linker.plugins.updater.config
{
    public sealed class UpdaterConfigInfo
    {
        private string runWindows = "sc start linker.service";
        private string stopWindows = "sc stop linker.service & taskkill /F /IM linker.exe & taskkill /F /IM linker.tray.win.exe";

        private string runLinux = "systemctl start linker";
        private string stopLinux = "systemctl stop linker";

        private string runOsx = "launchctl start linker";
        private string stopOsx = "launchctl stop linker";


        private string runCommand = string.Empty;
        public string RunCommand
        {
            get => runCommand; set
            {
                runCommand = value;
                if (string.IsNullOrWhiteSpace(runCommand))
                {
                    runCommand = OperatingSystem.IsWindows() ? runWindows : OperatingSystem.IsLinux() ? runLinux : runOsx;
                }
            }
        }

        private string stopCommand = string.Empty;
        public string StopCommand
        {
            get => stopCommand; set
            {
                stopCommand = value;
                if (string.IsNullOrWhiteSpace(stopCommand))
                {
                    stopCommand = OperatingSystem.IsWindows() ? stopWindows : OperatingSystem.IsLinux() ? stopLinux : stopOsx;
                }
            }
        }
    }
}


namespace linker.config
{
    public sealed partial class ConfigClientInfo
    {
        public UpdaterConfigInfo Updater { get; set; } = new UpdaterConfigInfo();
    }
}