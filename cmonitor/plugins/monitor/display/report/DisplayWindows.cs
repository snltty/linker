using common.libs.helpers;
using System.Runtime.Versioning;

namespace cmonitor.plugins.display.report
{
    [SupportedOSPlatform("windows")]
    public sealed class DisplayWindows : IDisplay
    {
        public DisplayWindows()
        {
        }

        public void SetDisplayState(bool onState)
        {
            if (onState)
            {
                DisplayHelper.On();
            }
            else
            {
                DisplayHelper.Off();
            }
        }
    }
}
