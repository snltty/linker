using common.libs.helpers;

namespace cmonitor.plugins.screen.report
{
    public interface IScreen
    {
        public DisplayInfo[] GetDisplays(out int w, out int h);
        public uint GetLastInputTime();


        public void Clip(ScreenClipInfo _screenClipInfo);
        public bool IsClip();

        public DesktopFrame GetClipFrame();
        public DesktopFrame GetFullFrame();
        public DesktopFrame GetRegionFrame();

        public void WakeUp();


    }
}
