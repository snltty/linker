using common.libs;
#if DEBUG || RELEASE
using Microsoft.Win32;
#endif

namespace cmonitor.server.client.reports.llock
{
    public sealed class UsbReport : IReport
    {
        public string Name => "Usb";
        private UsbReportInfo report = new UsbReportInfo();
        public UsbReport(Config config)
        {
            if (config.IsCLient)
            {
                UnLockUsb();
                report.Value = GetHasUSBDisabled();
                AppDomain.CurrentDomain.ProcessExit += (s, e) => UnLockUsb();
                Console.CancelKeyPress += (s, e) => UnLockUsb();
            }
        }

        public object GetReports()
        {
            return report;
        }

        public void Update(bool llock)
        {
            if (llock)
            {
                LockUsb();
            }
            else
            {
                UnLockUsb();
            }

            report.Value = GetHasUSBDisabled();
        }

        private bool GetHasUSBDisabled()
        {
#if DEBUG || RELEASE
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\\CurrentControlSet\\Services\\USBSTOR", true);
                    return key != null
                        && (int)key.GetValue("Start", 3, RegistryValueOptions.None) == 4;
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"get usb state {ex}");
            }
#endif
            return false;
        }
        private void LockUsb()
        {
#if DEBUG || RELEASE
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\\CurrentControlSet\\Services\\USBSTOR", true);
                    if (key != null)
                    {
                        key.SetValue("Start", 4);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"lock usb {ex}");
            }
#endif
        }
        private void UnLockUsb()
        {
#if DEBUG || RELEASE
            try
            {
                if (OperatingSystem.IsWindows())
                {

                    RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\\CurrentControlSet\\Services\\USBSTOR", true);
                    if (key != null)
                    {
                        key.SetValue("Start", 3);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"unlock usb {ex}");
            }
#endif
        }
    }

    public sealed class UsbReportInfo
    {
        public bool Value { get; set; }
    }
}
