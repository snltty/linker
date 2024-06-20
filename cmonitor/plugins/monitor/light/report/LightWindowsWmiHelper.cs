
using System.Management;
using common.libs;

namespace cmonitor.plugins.light.report
{
    public static class LightWindowsWmiHelper
    {
        public static int GetBrightnessLevel()
        {
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
                    //Logger.Instance.Error(ex);
                }
                catch (Exception)
                {
                    //Logger.Instance.Error(ex);
                }
            }
            return 0;
        }

        public static void SetBrightnessLevel(int brightnessLevel)
        {
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
                        uint.MaxValue,
                        brightnessLevel
                        });
                    }

                    moc.Dispose();
                    mos.Dispose();
                }
                catch (ManagementException)
                {
                    //  Logger.Instance.Error(ex);
                }
                catch (Exception)
                {
                    //  Logger.Instance.Error(ex);
                }
            }
        }
    }
}
