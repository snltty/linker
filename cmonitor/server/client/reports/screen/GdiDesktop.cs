using System.Drawing.Imaging;
using System.Drawing;
using common.libs;

namespace cmonitor.server.client.reports.screen
{
    public sealed class GdiDesktop
    {
        public GdiDesktop() { }

        ScreenClipInfo screenClipInfo = new ScreenClipInfo { X = 0, Y = 0, Scale = 1 };
        public void Clip(ScreenClipInfo _screenClipInfo)
        {
            screenClipInfo = _screenClipInfo;
        }
        public bool IsClip()
        {
            return screenClipInfo.Scale != 1;
        }
        public bool IsLockScreen()
        {
            return WinApi.Locked();
        }

        byte[] fullImageBytes = new byte[0];
        public DesktopFrame GetLatestFrame(float configScale)
        {
            DesktopFrame frame = new DesktopFrame { FullImage = Helper.EmptyArray, MovedRegions = new MovedRegion[0], RegionImage = Helper.EmptyArray, UpdatedRegions = new Rectangle[0] };

            if (OperatingSystem.IsWindows())
            {
                IntPtr hdc = WinApi.GetDC(IntPtr.Zero);
                if (hdc == IntPtr.Zero) return frame;

                GetSystemScale(out float scaleX, out float scaleY, out int sourceWidth, out int sourceHeight);
                Rect sourceRect = new Rect(sourceWidth, sourceHeight);
                CalcClip(sourceRect, out Rectangle clipRectangle);
                GetNewSize(sourceRect, scaleX, scaleY, configScale, out Rect distRect);

                using Bitmap image = new Bitmap(clipRectangle.Width, clipRectangle.Height);
                using (Graphics g = Graphics.FromImage(image))
                {
                    g.CopyFromScreen(clipRectangle.X, clipRectangle.Y, 0, 0, image.Size, CopyPixelOperation.SourceCopy);
                    g.Dispose();
                }
                WinApi.ReleaseDC(IntPtr.Zero, hdc);

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

            return frame;
        }


        private void CalcClip(Rect sourceRect, out Rectangle rectangle)
        {
            Scale scale = new Scale(screenClipInfo.X, screenClipInfo.Y, screenClipInfo.Scale);
            CalcClip(sourceRect, scale, out rectangle);

        }
        private void CalcClip(Rect sourceRect, Scale scale, out Rectangle rectangle)
        {
            //缩放后宽高
            int newSourceWidth = (int)(sourceRect.Width * scale.Value);
            int newSourceHeight = (int)(sourceRect.Height * scale.Value);

            //减去的宽高
            int clipWidth = (int)((newSourceWidth - sourceRect.Width) * 1.0 / newSourceWidth * sourceRect.Width);
            int clipHeight = (int)((newSourceHeight - sourceRect.Height) * 1.0 / newSourceHeight * sourceRect.Height);
            //留下的宽高
            int width = sourceRect.Width - clipWidth;
            int height = sourceRect.Height - clipHeight;

            float scaleX = scale.X * 1.0f / sourceRect.Width;
            float scaleY = scale.Y * 1.0f / sourceRect.Height;

            int left = (int)(clipWidth * scaleX);
            int top = (int)(clipHeight * scaleY);

            rectangle = new Rectangle(left, top, width, height);

        }

        private bool GetSystemScale(out float x, out float y, out int sourceWidth, out int sourceHeight)
        {
            x = 1;
            y = 1;
            sourceWidth = 0;
            sourceHeight = 0;
            IntPtr hdc = WinApi.GetDC(IntPtr.Zero);
            if (hdc != IntPtr.Zero)
            {
                sourceWidth = WinApi.GetDeviceCaps(hdc, WinApi.DESKTOPHORZRES);
                sourceHeight = WinApi.GetDeviceCaps(hdc, WinApi.DESKTOPVERTRES);
                int screenWidth = WinApi.GetSystemMetrics(WinApi.SM_CXSCREEN);
                int screenHeight = WinApi.GetSystemMetrics(WinApi.SM_CYSCREEN);

                x = (sourceWidth * 1.0f / screenWidth);
                y = (sourceHeight * 1.0f / screenHeight);

                WinApi.ReleaseDC(IntPtr.Zero, hdc);

                return true;
            }
            return false;
        }
        private bool GetNewSize(Rect sourceRect, float scaleX, float scaleY, float configScale, out Rect rect)
        {
            int width = (int)(sourceRect.Width * 1.0 / scaleX * configScale);
            int height = (int)(sourceRect.Height * 1.0 / scaleY * configScale);
            rect = new Rect(width, height);
            return true;
        }
    }
}
