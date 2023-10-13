using System.Drawing;
using System.Drawing.Imaging;

namespace cmonitor.server.client.reports.screen.aforge
{
    public static class Image
    {
        public static bool IsGrayscale(Bitmap image)
        {

            bool ret = false;
            if (OperatingSystem.IsWindows())
            {
                if (image.PixelFormat == PixelFormat.Format8bppIndexed)
                {
                    ret = true;
                    ColorPalette cp = image.Palette;
                    Color c;
                    for (int i = 0; i < 256; i++)
                    {
                        c = cp.Entries[i];
                        if ((c.R != i) || (c.G != i) || (c.B != i))
                        {
                            ret = false;
                            break;
                        }
                    }
                }
            }
            return ret;
        }

        public static Bitmap CreateGrayscaleImage(int width, int height)
        {
            if (OperatingSystem.IsWindows())
            {
                Bitmap image = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
                SetGrayscalePalette(image);
                return image;
            }
            return null;
        }

        public static void SetGrayscalePalette(Bitmap image)
        {
            if (OperatingSystem.IsWindows())
            {
                if (image.PixelFormat != PixelFormat.Format8bppIndexed)
                    throw new Exception("Source image is not 8 bpp image.");

                ColorPalette cp = image.Palette;
                for (int i = 0; i < 256; i++)
                {
                    cp.Entries[i] = Color.FromArgb(i, i, i);
                }
                image.Palette = cp;
            }
        }

        public static int GetPixelFormatSize(PixelFormat pixfmt)
        {
            return ((int)pixfmt >> 8) & 0xFF;
        }
    }
}
