using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using SharpDX.Mathematics.Interop;
using common.libs;
using common.libs.extends;
using cmonitor.server.client.reports.screen.aforge;
using System.Drawing.Drawing2D;

namespace cmonitor.server.client.reports.screen.sharpDX
{
    public class DesktopDuplicator
    {
        private Adapter1 adapter;
        private Device mDevice;
        private OutputDescription mOutputDesc;
        private OutputDuplication mDeskDupl;
        private Output output;
        private Output1 output1;

        private Texture2D desktopImageTexture = null;
        private Texture2D smallerTexture = null;
        private ShaderResourceView smallerTextureView = null;
        private OutputDuplicateFrameInformation frameInfo = new OutputDuplicateFrameInformation();

        public DesktopDuplicator(int whichMonitor)
            : this(0, whichMonitor) { }

        public DesktopDuplicator(int whichGraphicsCardAdapter, int whichOutputDevice)
        {
            if (OperatingSystem.IsWindows())
            {
                InitCapture(whichGraphicsCardAdapter, whichOutputDevice);
            }
        }

        private void InitCapture(int whichGraphicsCardAdapter, int whichOutputDevice)
        {
            try
            {
                try
                {
                    if (adapter != null)
                    {
                        adapter.Dispose();
                        adapter = null;
                    }
                }
                catch (Exception)
                {
                }
                adapter = new Factory1().GetAdapter1(whichGraphicsCardAdapter);
            }
            catch (SharpDXException)
            {
                throw new Exception("Could not find the specified graphics card adapter.");
            }

            try
            {
                if (mDevice != null)
                {
                    mDevice.Dispose();
                    mDevice = null;
                }
            }
            catch (Exception)
            {
            }
            mDevice = new Device(adapter);
           
            
            try
            {
                try
                {
                    if (output != null)
                    {
                        output.Dispose();
                        output = null;
                    }
                }
                catch (Exception)
                {
                }
                output = adapter.GetOutput(whichOutputDevice);
            }
            catch (SharpDXException)
            {
                throw new Exception("Could not find the specified output device.");
            }

            try
            {
                if (output1 != null)
                {
                    output1.Dispose();
                    output1 = null;
                }
            }
            catch (Exception)
            {
            }
            output1 = output.QueryInterface<Output1>();
            this.mOutputDesc = output.Description;
            int width = output1.Description.DesktopBounds.Right - output1.Description.DesktopBounds.Left;
            int height = output1.Description.DesktopBounds.Bottom - output1.Description.DesktopBounds.Top;


            try
            {
                if (desktopImageTexture != null)
                {
                    desktopImageTexture.Dispose();
                    desktopImageTexture = null;
                }
            }
            catch (Exception)
            {
            }
            desktopImageTexture = new Texture2D(mDevice, new Texture2DDescription()
            {
                CpuAccessFlags = CpuAccessFlags.Read,
                BindFlags = BindFlags.None,
                Format = Format.B8G8R8A8_UNorm,
                Width = width,
                Height = height,
                OptionFlags = ResourceOptionFlags.None,
                MipLevels = 1,
                ArraySize = 1,
                SampleDescription = { Count = 1, Quality = 0 },
                Usage = ResourceUsage.Staging
            });

            try
            {
                if (smallerTexture != null)
                {
                    smallerTexture.Dispose();
                    smallerTexture = null;
                }
            }
            catch (Exception)
            {
            }
            smallerTexture = new Texture2D(mDevice, new Texture2DDescription
            {
                CpuAccessFlags = CpuAccessFlags.None,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                Format = Format.B8G8R8A8_UNorm,
                Width = width,
                Height = height,
                OptionFlags = ResourceOptionFlags.GenerateMipMaps,
                MipLevels = 4,
                ArraySize = 1,
                SampleDescription = { Count = 1, Quality = 0 },
                Usage = ResourceUsage.Default
            });

            try
            {
                if (smallerTextureView != null)
                {
                    smallerTextureView.Dispose();
                    smallerTextureView = null;
                }
            }
            catch (Exception)
            {
            }
            smallerTextureView = new ShaderResourceView(mDevice, smallerTexture);

            try
            {
                if (mDeskDupl != null)
                {
                    mDeskDupl.Dispose();
                    mDeskDupl = null;
                }
                this.mDeskDupl = output1.DuplicateOutput(mDevice);
            }
            catch (SharpDXException ex)
            {
                Logger.Instance.Error(ex);
                if (ex.ResultCode.Code == SharpDX.DXGI.ResultCode.NotCurrentlyAvailable.Result.Code)
                {
                    throw new Exception("There is already the maximum number of applications using the Desktop Duplication API running, please close one of the applications and try again.");
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
        }

        public DesktopFrame GetLatestFrame(ScreenReportType screenReportType, float configScale)
        {
            DesktopFrame frame = new DesktopFrame() { FullImage = Helper.EmptyArray, RegionImage = Helper.EmptyArray };
            bool retrievalTimedOut = RetrieveFrame(frame, configScale);
            if (retrievalTimedOut)
            {
                return null;
            }

            try
            {
                RetrieveFrameMetadata(frame);
                if (frame.UpdatedRegions.Length > 0 || GdiCapture.IsClip())
                {
                    if (screenReportType == ScreenReportType.Full)
                    {
                        ProcessFrameFull(frame, configScale);
                    }
                    else if (screenReportType == ScreenReportType.Region)
                    {
                        frame.UpdatedRegions = Rectangle.UnionRectangles(frame.UpdatedRegions);
                        ProcessFrameRegion(frame, configScale);
                    }
                }
            }
            catch
            {
                ReleaseFrame();
            }
            finally
            {
                try
                {
                    ReleaseFrame();
                }
                catch
                {
                }
            }

            return frame;
        }

        private bool RetrieveFrame(DesktopFrame frame, float configScale)
        {
            SharpDX.DXGI.Resource desktopResource = null;
            try
            {
                mDeskDupl.TryAcquireNextFrame(500, out frameInfo, out desktopResource);
                if (desktopResource == null)
                {
                    InitCapture(0,0);
                    return false;
                }
            }
            catch (SharpDXException ex)
            {
                if (ex.ResultCode.Code == SharpDX.DXGI.ResultCode.WaitTimeout.Result.Code)
                {
                    return true;
                }
                if (ex.ResultCode.Failure)
                {
                    throw new Exception("Failed to acquire next frame.");
                }
            }

            int sourceWidth = smallerTexture.Description.Width;
            int sourceHeight = smallerTexture.Description.Height;

            frame.Width = sourceWidth;
            frame.Height = sourceHeight;

            GdiCapture.GetScale(out float scalex, out float scaley, out int sourceWidth1, out int sourceHeight1);
            frame.ScaleX = scalex;
            frame.ScaleY = scaley;

            GdiCapture.CalcClip(sourceWidth, sourceHeight, out int left, out int top, out int _width, out int _height);

            frame.Width = _width;
            frame.Height = _height;

            using Texture2D tempTexture = desktopResource.QueryInterface<Texture2D>();
            mDevice.ImmediateContext.CopySubresourceRegion(tempTexture, 0, new ResourceRegion
            {
                Left = left,
                Top = top,
                Right = left + _width,
                Bottom = top + _height,
                Back = 1
            }, smallerTexture, 0);

            desktopResource.Dispose();
            return false;
        }
        private void RetrieveFrameMetadata(DesktopFrame frame)
        {
            if (frameInfo.TotalMetadataBufferSize > 0)
            {
                /*
                OutputDuplicateMoveRectangle[] movedRectangles = new OutputDuplicateMoveRectangle[frameInfo.TotalMetadataBufferSize];
                mDeskDupl.GetFrameMoveRects(movedRectangles.Length, movedRectangles, out int movedRegionsLength);
                //Console.WriteLine($"movedRegionsLength:{movedRegionsLength}");
                frame.MovedRegions = new MovedRegion[movedRegionsLength / Marshal.SizeOf(typeof(OutputDuplicateMoveRectangle))];
                for (int i = 0; i < frame.MovedRegions.Length; i++)
                {
                    int width = movedRectangles[i].DestinationRect.Right - movedRectangles[i].DestinationRect.Left;
                    int height = movedRectangles[i].DestinationRect.Bottom - movedRectangles[i].DestinationRect.Top;
                    frame.MovedRegions[i] = new MovedRegion()
                    {
                        Source = new System.Drawing.Point(movedRectangles[i].SourcePoint.X, movedRectangles[i].SourcePoint.Y),
                        Destination = new Rectangle(movedRectangles[i].DestinationRect.Left, movedRectangles[i].DestinationRect.Top, width, height)
                    };

                }
                */

                RawRectangle[] dirtyRectangles = new RawRectangle[frameInfo.TotalMetadataBufferSize];
                mDeskDupl.GetFrameDirtyRects(dirtyRectangles.Length, dirtyRectangles, out int dirtyRegionsLength);
                frame.UpdatedRegions = new Rectangle[dirtyRegionsLength / Marshal.SizeOf(typeof(Rectangle))];
                for (int i = 0; i < frame.UpdatedRegions.Length; i++)
                {
                    int width = dirtyRectangles[i].Right - dirtyRectangles[i].Left;
                    int height = dirtyRectangles[i].Bottom - dirtyRectangles[i].Top;
                    frame.UpdatedRegions[i] = new Rectangle(dirtyRectangles[i].Left, dirtyRectangles[i].Top, width, height);
                }
            }
            else
            {
                //frame.MovedRegions = new MovedRegion[0];
                frame.UpdatedRegions = new Rectangle[0];
            }
        }

        byte[] fullImageBytes = Helper.EmptyArray;
        private unsafe void ProcessFrameFull(DesktopFrame frame, float configScale)
        {
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    mDevice.ImmediateContext.GenerateMips(smallerTextureView);
                    GdiCapture.GetNewSize(smallerTexture.Description.Width, smallerTexture.Description.Height, frame.ScaleX, frame.ScaleY, configScale, out int width, out int height);

                    int sourceSubresource = frame.Width / width;
                    if (sourceSubresource >= 0)
                    {
                        while (sourceSubresource > 0 && smallerTexture.Description.Width / (1 << sourceSubresource) < width)
                        {
                            sourceSubresource--;
                        }
                        if (sourceSubresource >= 3) sourceSubresource = 3;
                        frame.Width = smallerTexture.Description.Width / (1 << sourceSubresource);
                        frame.Height = smallerTexture.Description.Height / (1 << sourceSubresource);
                    }

                    mDevice.ImmediateContext.CopySubresourceRegion(smallerTexture, sourceSubresource, new ResourceRegion
                    {
                        Left = 0,
                        Right = frame.Width,
                        Top = 0,
                        Bottom = frame.Height,
                        Back = 1
                    }, desktopImageTexture, 0);

                   
                    DataBox mapSource = mDevice.ImmediateContext.MapSubresource(desktopImageTexture, 0, MapMode.Read, MapFlags.None);
                    using Bitmap image = new Bitmap(frame.Width, frame.Height, PixelFormat.Format32bppArgb);
                    System.Drawing.Rectangle boundsRect = new System.Drawing.Rectangle(0, 0, frame.Width, frame.Height);
                    BitmapData mapDest = image.LockBits(boundsRect, ImageLockMode.WriteOnly, image.PixelFormat);
                    nint sourcePtr = mapSource.DataPointer;
                    nint destPtr = mapDest.Scan0;
                    for (int y = 0; y < frame.Height; y++)
                    {
                        Utilities.CopyMemory(destPtr, sourcePtr, frame.Width * 4);

                        sourcePtr = IntPtr.Add(sourcePtr, mapSource.RowPitch);
                        destPtr = IntPtr.Add(destPtr, mapDest.Stride);
                    }
                    image.UnlockBits(mapDest);
                    mDevice.ImmediateContext.UnmapSubresource(desktopImageTexture, 0);

                    Bitmap bmp = image;
                    if (frame.Width - width > 50)
                    {
                        bmp = new Bitmap(width, height);
                        using Graphics graphic = Graphics.FromImage(bmp);
                        graphic.DrawImage(image, new System.Drawing.Rectangle(0, 0, width, height), 0, 0, frame.Width, frame.Height, GraphicsUnit.Pixel);
                    }

                    using System.Drawing.Image image1 = bmp;

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
                    desktopImageTexture.Dispose();
                    desktopImageTexture = new Texture2D(mDevice, new Texture2DDescription()
                    {
                        CpuAccessFlags = CpuAccessFlags.Read,
                        BindFlags = BindFlags.None,
                        Format = Format.B8G8R8A8_UNorm,
                        Width = smallerTexture.Description.Width,
                        Height = smallerTexture.Description.Height,
                        OptionFlags = ResourceOptionFlags.None,
                        MipLevels = 1,
                        ArraySize = 1,
                        SampleDescription = { Count = 1, Quality = 0 },
                        Usage = ResourceUsage.Staging
                    });
                }
            }
        }

        byte[] regionImageBytes = Helper.EmptyArray;
        private unsafe void ProcessFrameRegion(DesktopFrame frame, float configScale)
        {
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    var mapSource = mDevice.ImmediateContext.MapSubresource(desktopImageTexture, 0, MapMode.Read, MapFlags.None);
                    int sourceWidth = mOutputDesc.DesktopBounds.Right - mOutputDesc.DesktopBounds.Left;
                    int sourceHeight = mOutputDesc.DesktopBounds.Bottom - mOutputDesc.DesktopBounds.Top;

                    using Bitmap image = new Bitmap(sourceWidth, sourceHeight, PixelFormat.Format32bppArgb);
                    System.Drawing.Rectangle boundsRect = new System.Drawing.Rectangle(0, 0, sourceWidth, sourceHeight);
                    var mapDest = image.LockBits(boundsRect, ImageLockMode.WriteOnly, image.PixelFormat);
                    var sourcePtr = mapSource.DataPointer;
                    var destPtr = mapDest.Scan0;
                    for (int y = 0; y < sourceHeight; y++)
                    {
                        Utilities.CopyMemory(destPtr, sourcePtr, sourceWidth * 4);

                        sourcePtr = IntPtr.Add(sourcePtr, mapSource.RowPitch);
                        destPtr = IntPtr.Add(destPtr, mapDest.Stride);
                    }
                    image.UnlockBits(mapDest);
                    mDevice.ImmediateContext.UnmapSubresource(desktopImageTexture, 0);

                    Rectangle[] updatedRegions = frame.UpdatedRegions;


                    /*
                    if (diffFilter.OverlayImage == null)
                    {

                        diffFilter.OverlayImage = image;
                        blobCounter = new BlobCounter();
                        blobCounter.FilterBlobs = true;
                        blobCounter.MinHeight = 10;
                        blobCounter.MinWidth = 10;
                        blobCounter.ObjectsOrder = ObjectsOrder.Area;
                    }
                    else
                    {
                        var sw = new Stopwatch();
                        sw.Start();

                        // 获取差异图像
                        Bitmap diffImage = diffFilter.Apply(image);
                        blobCounter.ProcessImage(diffImage);
                        Blob[] blobs = blobCounter.GetObjectsInformation();

                        frame.Updateds = new Rectangle[blobs.Length];

                        sw.Stop();
                        Console.WriteLine($"========================================{sw.ElapsedMilliseconds}->{blobs.Length}");
                        for (int i = 0; i < blobs.Length; i++)
                        {
                            frame.Updateds[i] = new Rectangle(blobs[i].Rectangle.X, blobs[i].Rectangle.Y, blobs[i].Rectangle.Width, blobs[i].Rectangle.Height);
                            Console.WriteLine($"{blobs[i].Rectangle.X}->{blobs[i].Rectangle.Y}->{blobs[i].Rectangle.Width}->{blobs[i].Rectangle.Height}");
                        }

                        diffFilter.OverlayImage.Dispose();
                        diffFilter.OverlayImage = image;

                    }
                    Rectangle[] updatedRegions = frame.Updateds;
                    */
                    /*
                    int index = 0;
                    for (int i = 0; i < updatedRegions.Length; i++)
                    {
                        Rectangle region = updatedRegions[i];

                        int width = (int)(region.Width * configScale);
                        int height = (int)(region.Height * configScale);

                        using System.Drawing.Image image1 = bmp;
                        using MemoryStream ms = new MemoryStream();
                        image1.Save(ms, ImageFormat.Jpeg);
                        ms.Seek(0, SeekOrigin.Begin);

                        int msLength = (int)ms.Length;
                        ResizeRegionBytes(index, msLength + 4 * 5);

                        msLength.ToBytes(regionImageBytes.AsMemory(index));
                        index += 4;

                        region.X.ToBytes(regionImageBytes.AsMemory(index));
                        index += 4;
                        region.Width.ToBytes(regionImageBytes.AsMemory(index));
                        index += 4;

                        region.Y.ToBytes(regionImageBytes.AsMemory(index));
                        index += 4;
                        region.Height.ToBytes(regionImageBytes.AsMemory(index));
                        index += 4;

                        ms.Read(regionImageBytes.AsSpan(index));
                        index += msLength;
                    }
                    frame.RegionImage = regionImageBytes.AsMemory(0, index);
                    */
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
        private void ResizeRegionBytes(int index, int length)
        {
            if (regionImageBytes.Length < index + length)
            {
                byte[] bytes = new byte[index + length];
                regionImageBytes.AsMemory(0, index).CopyTo(bytes.AsMemory());
                regionImageBytes = bytes;
            }
        }

        private void ReleaseFrame()
        {
            try
            {
                mDeskDupl.ReleaseFrame();
            }
            catch (SharpDXException ex)
            {
                if (ex.ResultCode.Failure)
                {
                    throw new Exception("Failed to release frame.");
                }
            }
        }
    }
}
