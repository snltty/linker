using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;

namespace cmonitor.server.client.reports.screen.aforge
{


    public class UnmanagedImage : IDisposable
    {
        private IntPtr imageData;
        private int width, height;
        private int stride;
        private PixelFormat pixelFormat;
        private bool mustBeDisposed = false;

        public IntPtr ImageData
        {
            get { return imageData; }
        }

        public int Width
        {
            get { return width; }
        }

        public int Height
        {
            get { return height; }
        }

        public int Stride
        {
            get { return stride; }
        }

        public PixelFormat PixelFormat
        {
            get { return pixelFormat; }
        }

        public UnmanagedImage( IntPtr imageData, int width, int height, int stride, PixelFormat pixelFormat )
        {
            this.imageData   = imageData;
            this.width       = width;
            this.height      = height;
            this.stride      = stride;
            this.pixelFormat = pixelFormat;
        }

        public UnmanagedImage( BitmapData bitmapData )
        {
            if (OperatingSystem.IsWindows())
            {
                this.imageData = bitmapData.Scan0;
                this.width = bitmapData.Width;
                this.height = bitmapData.Height;
                this.stride = bitmapData.Stride;
                this.pixelFormat = bitmapData.PixelFormat;
            }
        }

        ~UnmanagedImage( )
        {
            Dispose( false );
        }

        public void Dispose( )
        {
            Dispose( true );
            // remove me from the Finalization queue 
            GC.SuppressFinalize( this );
        }

        protected virtual void Dispose( bool disposing )
        {
            if ( disposing )
            {
                // dispose managed resources
            }
            // free image memory if the image was allocated using this class
            if ( ( mustBeDisposed ) && ( imageData != IntPtr.Zero ) )
            {
                System.Runtime.InteropServices.Marshal.FreeHGlobal( imageData );
                System.GC.RemoveMemoryPressure( stride * height );
                imageData = IntPtr.Zero;
            }
        }

       

        public static UnmanagedImage Create( int width, int height, PixelFormat pixelFormat )
        {
            if (OperatingSystem.IsWindows())
            {
                int bytesPerPixel = 0;

                // calculate bytes per pixel
                switch (pixelFormat)
                {
                    case PixelFormat.Format8bppIndexed:
                        bytesPerPixel = 1;
                        break;
                    case PixelFormat.Format16bppGrayScale:
                        bytesPerPixel = 2;
                        break;
                    case PixelFormat.Format24bppRgb:
                        bytesPerPixel = 3;
                        break;
                    case PixelFormat.Format32bppRgb:
                    case PixelFormat.Format32bppArgb:
                    case PixelFormat.Format32bppPArgb:
                        bytesPerPixel = 4;
                        break;
                    case PixelFormat.Format48bppRgb:
                        bytesPerPixel = 6;
                        break;
                    case PixelFormat.Format64bppArgb:
                    case PixelFormat.Format64bppPArgb:
                        bytesPerPixel = 8;
                        break;
                    default:
                        throw new Exception("Can not create image with specified pixel format.");
                }

                // check image size
                if ((width <= 0) || (height <= 0))
                {
                    throw new Exception("Invalid image size specified.");
                }

                // calculate stride
                int stride = width * bytesPerPixel;

                if (stride % 4 != 0)
                {
                    stride += (4 - (stride % 4));
                }

                // allocate memory for the image
                IntPtr imageData = System.Runtime.InteropServices.Marshal.AllocHGlobal(stride * height);
                cmonitor.server.client.reports.screen.aforge.SystemTools.SetUnmanagedMemory(imageData, 0, stride * height);
                System.GC.AddMemoryPressure(stride * height);

                UnmanagedImage image = new UnmanagedImage(imageData, width, height, stride, pixelFormat);
                image.mustBeDisposed = true;

                return image;
            }
            return null;
        }
        

        public void SetPixel( IntPoint point, Color color )
        {
            SetPixel( point.X, point.Y, color );
        }

        public void SetPixel( int x, int y, Color color )
        {
            SetPixel( x, y, color.R, color.G, color.B, color.A );
        }

        public void SetPixel( int x, int y, byte value )
        {
            SetPixel( x, y, value, value, value, 255 );
        }

        private void SetPixel( int x, int y, byte r, byte g, byte b, byte a )
        {
            if (OperatingSystem.IsWindows())
            {
                if ((x >= 0) && (y >= 0) && (x < width) && (y < height))
                {
                    unsafe
                    {
                        int pixelSize = Bitmap.GetPixelFormatSize(pixelFormat) / 8;
                        byte* ptr = (byte*)imageData.ToPointer() + y * stride + x * pixelSize;
                        ushort* ptr2 = (ushort*)ptr;

                        switch (pixelFormat)
                        {
                            case PixelFormat.Format8bppIndexed:
                                *ptr = (byte)(0.2125 * r + 0.7154 * g + 0.0721 * b);
                                break;

                            case PixelFormat.Format24bppRgb:
                            case PixelFormat.Format32bppRgb:
                                ptr[RGB.R] = r;
                                ptr[RGB.G] = g;
                                ptr[RGB.B] = b;
                                break;

                            case PixelFormat.Format32bppArgb:
                                ptr[RGB.R] = r;
                                ptr[RGB.G] = g;
                                ptr[RGB.B] = b;
                                ptr[RGB.A] = a;
                                break;

                            case PixelFormat.Format16bppGrayScale:
                                *ptr2 = (ushort)((ushort)(0.2125 * r + 0.7154 * g + 0.0721 * b) << 8);
                                break;

                            case PixelFormat.Format48bppRgb:
                                ptr2[RGB.R] = (ushort)(r << 8);
                                ptr2[RGB.G] = (ushort)(g << 8);
                                ptr2[RGB.B] = (ushort)(b << 8);
                                break;

                            case PixelFormat.Format64bppArgb:
                                ptr2[RGB.R] = (ushort)(r << 8);
                                ptr2[RGB.G] = (ushort)(g << 8);
                                ptr2[RGB.B] = (ushort)(b << 8);
                                ptr2[RGB.A] = (ushort)(a << 8);
                                break;

                            default:
                                throw new Exception("The pixel format is not supported: " + pixelFormat);
                        }
                    }
                }
            }
        }

        public Color GetPixel( IntPoint point )
        {
            return GetPixel( point.X, point.Y );
        }

        public Color GetPixel( int x, int y )
        {
            if (OperatingSystem.IsWindows())
            {
                if ((x < 0) || (y < 0))
                {
                    throw new ArgumentOutOfRangeException("x", "The specified pixel coordinate is out of image's bounds.");
                }

                if ((x >= width) || (y >= height))
                {
                    throw new ArgumentOutOfRangeException("y", "The specified pixel coordinate is out of image's bounds.");
                }

                Color color = new Color();

                unsafe
                {
                    int pixelSize = Bitmap.GetPixelFormatSize(pixelFormat) / 8;
                    byte* ptr = (byte*)imageData.ToPointer() + y * stride + x * pixelSize;

                    switch (pixelFormat)
                    {
                        case PixelFormat.Format8bppIndexed:
                            color = Color.FromArgb(*ptr, *ptr, *ptr);
                            break;

                        case PixelFormat.Format24bppRgb:
                        case PixelFormat.Format32bppRgb:
                            color = Color.FromArgb(ptr[RGB.R], ptr[RGB.G], ptr[RGB.B]);
                            break;

                        case PixelFormat.Format32bppArgb:
                            color = Color.FromArgb(ptr[RGB.A], ptr[RGB.R], ptr[RGB.G], ptr[RGB.B]);
                            break;

                        default:
                            throw new Exception("The pixel format is not supported: " + pixelFormat);
                    }
                }

                return color;
            }
            return new Color();
        }
    }
}
