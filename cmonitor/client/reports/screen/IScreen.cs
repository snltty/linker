using common.libs.helpers;

namespace cmonitor.client.reports.screen
{
    public interface IScreen
    {
        public DisplayInfo[] GetDisplays(out int w, out int h);
        public void SetDisplayState(bool onState);
        public uint GetLastInputTime();


        public void Clip(ScreenClipInfo _screenClipInfo);
        public bool IsClip();

        public DesktopFrame GetClipFrame();
        public DesktopFrame GetFullFrame();
        public DesktopFrame GetRegionFrame();

        public void WakeUp();


        public void ScreenShareState(ScreenShareStates screenShareState);
        public void ScreenShare(Memory<byte> data);

    }
}
