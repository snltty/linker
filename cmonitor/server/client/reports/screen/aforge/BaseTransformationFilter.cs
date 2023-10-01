using System.Drawing;
using System.Drawing.Imaging;

namespace cmonitor.server.client.reports.screen.aforge
{


    public abstract class BaseTransformationFilter
    {
        public abstract Dictionary<PixelFormat, PixelFormat> FormatTranslations { get; }

        public Bitmap Apply(Bitmap image)
        {
            if (OperatingSystem.IsWindows())
            {
                BitmapData srcData = image.LockBits(
                new Rectangle(0, 0, image.Width, image.Height),
                ImageLockMode.ReadOnly, image.PixelFormat);

                Bitmap dstImage = null;

                try
                {
                    dstImage = Apply(srcData);
                    if ((image.HorizontalResolution > 0) && (image.VerticalResolution > 0))
                    {
                        dstImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);
                    }
                }
                finally
                {
                    image.UnlockBits(srcData);
                }

                return dstImage;
            }
            return null;
        }

        public Bitmap Apply(BitmapData imageData)
        {
            if (OperatingSystem.IsWindows())
            {
                CheckSourceFormat(imageData.PixelFormat);

                PixelFormat dstPixelFormat = FormatTranslations[imageData.PixelFormat];

                Size newSize = CalculateNewImageSize();

                Bitmap dstImage = (dstPixelFormat == PixelFormat.Format8bppIndexed) ?
                    cmonitor.server.client.reports.screen.aforge.Image.CreateGrayscaleImage(newSize.Width, newSize.Height) :
                    new Bitmap(newSize.Width, newSize.Height, dstPixelFormat);

                BitmapData dstData = dstImage.LockBits(
                    new Rectangle(0, 0, newSize.Width, newSize.Height),
                    ImageLockMode.ReadWrite, dstPixelFormat);

                try
                {
                    ProcessFilter(new UnmanagedImage(imageData), new UnmanagedImage(dstData));
                }
                finally
                {
                    dstImage.UnlockBits(dstData);
                }

                return dstImage;
            }
            return null;
        }

        public UnmanagedImage Apply(UnmanagedImage image)
        {
            if (OperatingSystem.IsWindows())
            {
                CheckSourceFormat(image.PixelFormat);

                Size newSize = CalculateNewImageSize();

                UnmanagedImage dstImage = UnmanagedImage.Create(newSize.Width, newSize.Height, FormatTranslations[image.PixelFormat]);

                ProcessFilter(image, dstImage);

                return dstImage;
            }
            return null;
        }

        public void Apply(UnmanagedImage sourceImage, UnmanagedImage destinationImage)
        {
            if (OperatingSystem.IsWindows())
            {
                CheckSourceFormat(sourceImage.PixelFormat);

                if (destinationImage.PixelFormat != FormatTranslations[sourceImage.PixelFormat])
                {
                    throw new Exception("Destination pixel format is specified incorrectly.");
                }

                Size newSize = CalculateNewImageSize();

                if ((destinationImage.Width != newSize.Width) || (destinationImage.Height != newSize.Height))
                {
                    throw new Exception("Destination image must have the size expected by the filter.");
                }

                ProcessFilter(sourceImage, destinationImage);
            }
        }

        protected abstract System.Drawing.Size CalculateNewImageSize();

        protected abstract unsafe void ProcessFilter(UnmanagedImage sourceData, UnmanagedImage destinationData);

        private void CheckSourceFormat(PixelFormat pixelFormat)
        {
            if (OperatingSystem.IsWindows())
            {
                if (!FormatTranslations.ContainsKey(pixelFormat))
                    throw new Exception("Source pixel format is not supported by the filter.");
            }
        }
    }
}
