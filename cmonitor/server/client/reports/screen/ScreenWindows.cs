using common.libs;
using common.libs.helpers;
using common.libs.winapis;
using System.IO.MemoryMappedFiles;

namespace cmonitor.server.client.reports.screen
{
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


        MemoryMappedFile mmf;
        MemoryMappedViewAccessor accessor;
        byte[] shareScreenBytes = new byte[1 * 1024 * 1024];
        private void ScreenShareInit()
        {
            if (OperatingSystem.IsWindows())
            {
               // mmf = MemoryMappedFile.CreateOrOpen($"{config.ShareMemoryKey}/screen", shareScreenBytes.Length);
               // accessor = mmf.CreateViewAccessor();
            }
        }
        public void ScreenShareState(ScreenShareStates screenShareState)
        {
            //clientConfig.ScreenShareState = screenShareState;
        }
        public void ScreenShare(Memory<byte> data)
        {
            if (data.Length > 0 && data.Length <= shareScreenBytes.Length && accessor != null)
            {
                data.CopyTo(shareScreenBytes);
                accessor.WriteArray(0, shareScreenBytes, 0, data.Length);
            }
        }
    }
}
