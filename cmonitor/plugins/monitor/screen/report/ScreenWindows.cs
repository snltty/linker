using cmonitor.config;
using common.libs;
using common.libs.helpers;
using common.libs.winapis;
using System.Runtime.Versioning;

namespace cmonitor.plugins.screen.report
{
    [SupportedOSPlatform("windows")]
    public sealed class ScreenWindows : IScreen
    {
        private readonly ScreenWindowsDxgi dxgiDesktop;
        private readonly ScreenWindowsGdi gdiDesktop;
        public ScreenWindows(Config config)
        {
            dxgiDesktop = new ScreenWindowsDxgi(0, config);
            gdiDesktop = new ScreenWindowsGdi(config);
        }


        public DisplayInfo[] GetDisplays(out int w, out int h)
        {
            DisplayInfo[] displays = DisplaysEnumerationHelper.GetDisplays();
            DisplayHelper.GetSystemScale(out _, out _, out w, out h);
            return displays;
        }

        public uint GetLastInputTime()
        {
            return LastInputHelper.GetLastInputInfo();
        }


        public void Clip(ScreenClipInfo screenClipInfo)
        {
            gdiDesktop.Clip(screenClipInfo);
        }
        public bool IsClip()
        {
            return gdiDesktop.IsClip();
        }

        public DesktopFrame GetClipFrame()
        {
            return gdiDesktop.GetLatestFrame();
        }
        public DesktopFrame GetFullFrame()
        {
            return dxgiDesktop.GetLatestFullFrame();
        }
        public DesktopFrame GetRegionFrame()
        {
            return dxgiDesktop.GetLatestRegionFrame();
        }

        public void WakeUp()
        {
            try
            {
                if (CursorHelper.GetCursorPosition(out int x, out int y))
                {
                    User32.SetCursorPos(x + 1, y + 1);
                    MouseHelper.MouseMove(1, 1);
                }
            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    Logger.Instance.Error(ex);
                }
            }
        }

    }
}
