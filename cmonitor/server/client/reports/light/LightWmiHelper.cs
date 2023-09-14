#if DEBUG || RELEASE
using System.Management;
#endif
using common.libs;

namespace cmonitor.server.client.reports.light
{
    public static class LightWmiHelper
    {
        public static int GetBrightnessLevel()
        {
#if DEBUG || RELEASE
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    var s = new ManagementScope("root\\WMI");
                    var q = new SelectQuery("WmiMonitorBrightness");
                    var mos = new ManagementObjectSearcher(s, q);
                    var moc = mos.Get();

                    foreach (var managementBaseObject in moc)
                    {
                        foreach (var o in managementBaseObject.Properties)
                        {
                            if (o.Name == "CurrentBrightness")
                                return Convert.ToInt32(o.Value);
                        }
                    }

                    moc.Dispose();
                    mos.Dispose();
                }
                catch (ManagementException)
                {
                    // ignore
                    // it is possible that laptop lid is closed, and using external monitor
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
#endif
            return 0;
        }

        public static void SetBrightnessLevel(int brightnessLevel)
        {
#if DEBUG || RELEASE
            if (OperatingSystem.IsWindows())
            {
                if (brightnessLevel < 0 ||
                brightnessLevel > 100)
                    throw new ArgumentOutOfRangeException("brightnessLevel");

                try
                {
                    var s = new ManagementScope("root\\WMI");
                    var q = new SelectQuery("WmiMonitorBrightnessMethods");
                    var mos = new ManagementObjectSearcher(s, q);
                    var moc = mos.Get();

                    foreach (var managementBaseObject in moc)
                    {
                        var o = (ManagementObject)managementBaseObject;
                        o.InvokeMethod("WmiSetBrightness", new object[]
                        {
                        UInt32.MaxValue,
                        brightnessLevel
                        });
                    }

                    moc.Dispose();
                    mos.Dispose();
                }
                catch (ManagementException ex)
                {
                    Logger.Instance.Error(ex);
                    // ignore
                    // it is possible that laptop lid is closed, and using external monitor
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error(ex);
                }
            }
#endif
        }
    }
}
