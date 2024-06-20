using common.libs.helpers;

namespace cmonitor.plugins.screen.report
{
    public sealed class ScreenLinux : IScreen
    {
        public DisplayInfo[] GetDisplays(out int w, out int h)
        {
            w = 0; h = 0;
            return Array.Empty<DisplayInfo>();
        }
        public void Clip(ScreenClipInfo _screenClipInfo)
        {
        }
        public bool IsClip()
        {
            return false;
        }
        public uint GetLastInputTime()
        {
            return 0;
        }




        public DesktopFrame GetClipFrame()
        {
            return null;
        }
        public DesktopFrame GetFullFrame()
        {
            return null;
        }
        public DesktopFrame GetRegionFrame()
        {
            return null;
        }
        public void WakeUp()
        {
        }


    }
}
