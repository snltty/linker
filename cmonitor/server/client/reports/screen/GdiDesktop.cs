using System.Drawing.Imaging;
using System.Drawing;
using common.libs;
using cmonitor.server.client.reports.screen.helpers;
using cmonitor.server.client.reports.screen.winapiss;
using System.Runtime.InteropServices;

namespace cmonitor.server.client.reports.screen
{
    public sealed class GdiDesktop
    {
        private readonly Config config;
        public GdiDesktop(Config config)
        {
            this.config = config;
        }

        ScreenClipInfo screenClipInfo = new ScreenClipInfo { X = 0, Y = 0, W = 0, H = 0 };
        public void Clip(ScreenClipInfo _screenClipInfo)
        {
            screenClipInfo = _screenClipInfo;
        }
        public bool IsClip()
        {
            return screenClipInfo.W > 0 && screenClipInfo.H > 0;
        }

        byte[] fullImageBytes = new byte[0];
        public DesktopFrame GetLatestFrame()
        {
            DesktopFrame frame = new DesktopFrame { FullImage = Helper.EmptyArray, MovedRegions = new MovedRegion[0], RegionImage = Helper.EmptyArray, UpdatedRegions = new Rectangle[0] };


            if (OperatingSystem.IsWindows())
            {
                try
                {
                    if (config.Elevated == true && !Win32Interop.SwitchToInputDesktop())
                    {
                        var errCode = Marshal.GetLastWin32Error();
                        if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                            Logger.Instance.Error("Failed to switch to input desktop. Last Win32 error code: {errCode}", errCode);
                    }


                    IntPtr hdc = User32.GetDC(IntPtr.Zero);
                    if (hdc == IntPtr.Zero) return frame;

                    DisplayHelper.GetSystemScale(out float scaleX, out float scaleY, out int sourceWidth, out int sourceHeight);
                    Rect sourceRect = new Rect(sourceWidth, sourceHeight);
                    CalcClip(out Rectangle clipRectangle);
                    if (screenClipInfo.W == 0 || screenClipInfo.H == 0)
                    {
                        clipRectangle.Width = sourceWidth;
                        clipRectangle.Height = sourceHeight;
                    }
                    GetNewSize(sourceRect, scaleX, scaleY, out Rect distRect);

                    using Bitmap image = new Bitmap(clipRectangle.Width, clipRectangle.Height);
                    using (Graphics g = Graphics.FromImage(image))
                    {
                        g.CopyFromScreen(clipRectangle.X, clipRectangle.Y, 0, 0, image.Size, CopyPixelOperation.SourceCopy);
                        g.Dispose();
                    }
                    User32.ReleaseDC(IntPtr.Zero, hdc);

                    Bitmap bmp = image;
                    if (clipRectangle.Width - distRect.Width > 50)
                    {
                        bmp = new Bitmap(distRect.Width, distRect.Height);
                        using Graphics graphic = Graphics.FromImage(bmp);
                        graphic.DrawImage(image, new System.Drawing.Rectangle(0, 0, distRect.Width, distRect.Height), 0, 0, clipRectangle.Width, clipRectangle.Height, GraphicsUnit.Pixel);
                    }


                    using Image image1 = bmp;

                    using MemoryStream ms = new MemoryStream();
                    image1.Save(ms, ImageFormat.Jpeg);
                    ms.Seek(0, SeekOrigin.Begin);

                    int length = (int)ms.Length;
                    if (length > fullImageBytes.Length)
                    {
                        fullImageBytes = new byte[length];
                    }
                    ms.Read(fullImageBytes.AsSpan(0, length));
                    frame.FullImage = fullImageBytes.AsMemory(0, length);
                }
                catch (Exception ex)
                {
                    if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        Logger.Instance.Error(ex);
                    }
                }
            }

            return frame;
        }


        private void CalcClip(out Rectangle rectangle)
        {
            rectangle = new Rectangle(screenClipInfo.X, screenClipInfo.Y, screenClipInfo.W, screenClipInfo.H);

        }

        private bool GetNewSize(Rect sourceRect, float scaleX, float scaleY,  out Rect rect)
        {
            int width = (int)(sourceRect.Width * 1.0 / scaleX * config.ScreenScale);
            int height = (int)(sourceRect.Height * 1.0 / scaleY * config.ScreenScale);
            rect = new Rect(width, height);
            return true;
        }
    }
}
