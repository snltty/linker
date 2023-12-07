using common.libs.helpers;

namespace cmonitor.client.reports.screen
{
    public sealed class ScreenMacOS : IScreen
    {
        public DisplayInfo[] GetDisplays(out int w, out int h)
        {
            w = 0; h = 0;
            return Array.Empty<DisplayInfo>();
        }
        public void SetDisplayState(bool onState)
        {
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


        public void ScreenShareState(ScreenShareStates screenShareState)
        {
        }
        public void ScreenShare(Memory<byte> data)
        {
        }
    }
}
