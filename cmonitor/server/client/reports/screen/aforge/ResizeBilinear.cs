using System.Drawing.Imaging;
namespace cmonitor.server.client.reports.screen.aforge
{


    public class ResizeBilinear : BaseResizeFilter
    {
        private Dictionary<PixelFormat, PixelFormat> formatTranslations;

        public override Dictionary<PixelFormat, PixelFormat> FormatTranslations
        {
            get { return formatTranslations; }
        }
		public ResizeBilinear( int newWidth, int newHeight ) :
            base( newWidth, newHeight )
		{
            if (OperatingSystem.IsWindows())
            {
                formatTranslations = new Dictionary<PixelFormat, PixelFormat>();
                formatTranslations[PixelFormat.Format8bppIndexed] = PixelFormat.Format8bppIndexed;
                formatTranslations[PixelFormat.Format24bppRgb] = PixelFormat.Format24bppRgb;
                formatTranslations[PixelFormat.Format32bppRgb] = PixelFormat.Format32bppRgb;
                formatTranslations[PixelFormat.Format32bppArgb] = PixelFormat.Format32bppArgb;
            }
        }

        protected override unsafe void ProcessFilter( UnmanagedImage sourceData, UnmanagedImage destinationData )
        {
            if (OperatingSystem.IsWindows())
            {
                // get source image size
                int width = sourceData.Width;
                int height = sourceData.Height;

                int pixelSize = System.Drawing.Image.GetPixelFormatSize(sourceData.PixelFormat) / 8;
                int srcStride = sourceData.Stride;
                int dstOffset = destinationData.Stride - pixelSize * newWidth;
                double xFactor = (double)width / newWidth;
                double yFactor = (double)height / newHeight;

                // do the job
                byte* src = (byte*)sourceData.ImageData.ToPointer();
                byte* dst = (byte*)destinationData.ImageData.ToPointer();

                // coordinates of source points
                double ox, oy, dx1, dy1, dx2, dy2;
                int ox1, oy1, ox2, oy2;
                // width and height decreased by 1
                int ymax = height - 1;
                int xmax = width - 1;
                // temporary pointers
                byte* tp1, tp2;
                byte* p1, p2, p3, p4;

                // for each line
                for (int y = 0; y < newHeight; y++)
                {
                    // Y coordinates
                    oy = (double)y * yFactor;
                    oy1 = (int)oy;
                    oy2 = (oy1 == ymax) ? oy1 : oy1 + 1;
                    dy1 = oy - (double)oy1;
                    dy2 = 1.0 - dy1;

                    // get temp pointers
                    tp1 = src + oy1 * srcStride;
                    tp2 = src + oy2 * srcStride;

                    // for each pixel
                    for (int x = 0; x < newWidth; x++)
                    {
                        // X coordinates
                        ox = (double)x * xFactor;
                        ox1 = (int)ox;
                        ox2 = (ox1 == xmax) ? ox1 : ox1 + 1;
                        dx1 = ox - (double)ox1;
                        dx2 = 1.0 - dx1;

                        // get four points
                        p1 = tp1 + ox1 * pixelSize;
                        p2 = tp1 + ox2 * pixelSize;
                        p3 = tp2 + ox1 * pixelSize;
                        p4 = tp2 + ox2 * pixelSize;

                        // interpolate using 4 points
                        for (int i = 0; i < pixelSize; i++, dst++, p1++, p2++, p3++, p4++)
                        {
                            *dst = (byte)(
                                dy2 * (dx2 * (*p1) + dx1 * (*p2)) +
                                dy1 * (dx2 * (*p3) + dx1 * (*p4)));
                        }
                    }
                    dst += dstOffset;
                }
            }
        }
    }
}
