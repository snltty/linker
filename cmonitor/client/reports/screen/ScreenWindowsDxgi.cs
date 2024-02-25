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
using Factory1 = SharpDX.DXGI.Factory1;
using common.libs.extends;
using common.libs.winapis;
using common.libs.helpers;
using cmonitor.client.reports.screen.h264;
using FFmpeg.AutoGen.Abstractions;

namespace cmonitor.client.reports.screen
{
    public sealed class ScreenWindowsDxgi
    {
        private Adapter1 adapter;
        private Device mDevice;
        private OutputDescription mOutputDesc;
        private OutputDuplication mDeskDupl;
        private Output output;
        private Output1 output1;

        private Texture2D desktopImageTexture = null;
        private Texture2D smallerTexture = null;
        private Texture2D tempTexture;
        private ShaderResourceView smallerTextureView = null;
        private OutputDuplicateFrameInformation frameInfo = new OutputDuplicateFrameInformation();

        private int whichGraphicsCardAdapter;
        private int whichOutputDevice;

        private bool needInit = false;

        private readonly Config config;

        public ScreenWindowsDxgi(int whichMonitor, Config config)
            : this(0, whichMonitor, config) { }

        public ScreenWindowsDxgi(int whichGraphicsCardAdapter, int whichOutputDevice, Config config)
        {
            this.whichGraphicsCardAdapter = whichGraphicsCardAdapter;
            this.whichOutputDevice = whichOutputDevice;
            this.config = config;
            if (OperatingSystem.IsWindows())
            {
                //FFmpegHelper.Initialize();
                InitCapture();
                //InitEncoder();
            }
        }


        private void InitAdapter()
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
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    Logger.Instance.Error(ex);
            }

        }
        private void InitDevice()
        {
            try
            {
                if (mDevice != null)
                {
                    mDevice.Dispose();
                    mDevice = null;
                }
                mDevice = new Device(adapter);
            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    Logger.Instance.Error(ex);
            }

        }
        private void InitOutput()
        {
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
                mOutputDesc = output.Description;

            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    Logger.Instance.Error(ex);

            }
            try
            {
                if (output1 != null)
                {
                    output1.Dispose();
                    output1 = null;
                }
                output1 = output.QueryInterface<Output1>();
            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    Logger.Instance.Error(ex);
            }

        }
        private void InitTexture()
        {
            if (output1 == null) return;
            int width = output1.Description.DesktopBounds.Right - output1.Description.DesktopBounds.Left;
            int height = output1.Description.DesktopBounds.Bottom - output1.Description.DesktopBounds.Top;

            try
            {
                if (desktopImageTexture != null)
                {
                    desktopImageTexture.Dispose();
                    desktopImageTexture = null;
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
            }
            catch (Exception)
            {
            }


            try
            {
                if (smallerTexture != null)
                {
                    smallerTexture.Dispose();
                    smallerTexture = null;
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
            }
            catch (Exception)
            {
            }


            try
            {
                if (smallerTextureView != null)
                {
                    smallerTextureView.Dispose();
                    smallerTextureView = null;
                }
                smallerTextureView = new ShaderResourceView(mDevice, smallerTexture);
            }
            catch (Exception)
            {
            }

        }
        private void InitDesk()
        {
            try
            {
                if (mDeskDupl != null)
                {
                    mDeskDupl.Dispose();
                    mDeskDupl = null;
                }
                mDeskDupl = output1.DuplicateOutput(mDevice);
            }
            catch (SharpDXException ex)
            {
                if (ex.ResultCode.Code == SharpDX.DXGI.ResultCode.NotCurrentlyAvailable.Result.Code)
                {
                    if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        Logger.Instance.Error("There is already the maximum number of applications using the Desktop Duplication API running, please close one of the applications and try again.");
                }
            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    Logger.Instance.Error(ex);
            }
            //GCHelper.FlushMemory();
        }
        private void InitCapture()
        {
            try
            {
                if (config.Elevated)
                {
                    Win32Interop.SwitchToInputDesktop();
                }
                InitAdapter();
                InitDevice();
                InitOutput();
                InitTexture();
                InitDesk();
            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    Logger.Instance.Error(ex);
            }
        }
        private bool RetrieveFrame()
        {
            SharpDX.DXGI.Resource desktopResource = null;
            try
            {
                if (mDeskDupl == null)
                {
                    InitDesk();
                }
                if (mDeskDupl == null)
                {
                    return false;
                }

                Result result = mDeskDupl.TryAcquireNextFrame(30, out frameInfo, out desktopResource);
                if (desktopResource == null)
                {
                    uint code = (uint)result.Code;
                    //https://learn.microsoft.com/zh-cn/windows/win32/direct3ddxgi/dxgi-error
                    if (code == 0x887A0026 || code == 0x887A0001 || code == 0x887A0007)
                    {
                        InitDesk();
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    Logger.Instance.Error(ex);
                }
                InitDesk();
                return false;
            }

            if (tempTexture != null)
            {
                tempTexture.Dispose();
                tempTexture = null;
            }
            tempTexture = desktopResource.QueryInterface<Texture2D>();
            mDevice.ImmediateContext.CopySubresourceRegion(tempTexture, 0, null, smallerTexture, 0);

            desktopResource.Dispose();
            return true;
        }
        private void RetrieveFrameMetadata(DesktopFrame frame)
        {
            if (frameInfo.TotalMetadataBufferSize > 0)
            {

                OutputDuplicateMoveRectangle[] movedRectangles = new OutputDuplicateMoveRectangle[frameInfo.TotalMetadataBufferSize];
                mDeskDupl.GetFrameMoveRects(movedRectangles.Length, movedRectangles, out int movedRegionsLength);
                //Console.WriteLine($"TotalMetadataBufferSize:{frameInfo.TotalMetadataBufferSize}->movedRegionsLength:{movedRegionsLength}->");
                frame.MovedRegions = new MovedRegion[movedRegionsLength / Marshal.SizeOf(typeof(OutputDuplicateMoveRectangle))];
                for (int i = 0; i < frame.MovedRegions.Length; i++)
                {
                    int width = movedRectangles[i].DestinationRect.Right - movedRectangles[i].DestinationRect.Left;
                    int height = movedRectangles[i].DestinationRect.Bottom - movedRectangles[i].DestinationRect.Top;
                    frame.MovedRegions[i] = new MovedRegion()
                    {
                        Source = new Point(movedRectangles[i].SourcePoint.X, movedRectangles[i].SourcePoint.Y),
                        Destination = new Rectangle(movedRectangles[i].DestinationRect.Left, movedRectangles[i].DestinationRect.Top, width, height)
                    };

                }


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
                frame.MovedRegions = new MovedRegion[0];
                frame.UpdatedRegions = new Rectangle[0];
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
                    if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        Logger.Instance.Error($"Failed to release frame.{ex.ResultCode.Code}");
                    }
                }
            }
        }


        private byte[] fullImageBytes = Helper.EmptyArray;
        public DesktopFrame GetLatestFullFrame()
        {
            DesktopFrame frame = new DesktopFrame() { FullImage = Helper.EmptyArray, RegionImage = Helper.EmptyArray };
            bool result = false;
            try
            {
                if (config.Elevated == true && !Win32Interop.SwitchToInputDesktop())
                {
                    var errCode = Marshal.GetLastWin32Error();
                    if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        Logger.Instance.Error($"Failed to switch to input desktop. Last Win32 error code: {errCode}");
                }

                if (needInit)
                {
                    InitCapture();
                }


                result = RetrieveFrame();
                if (result == false)
                {
                    return frame;
                }

                RetrieveFrameMetadata(frame);
                if (frame.UpdatedRegions.Length > 0)
                {
                    frame.UpdatedRegions = Rectangle.UnionRectangles(frame.UpdatedRegions);
                }
                ProcessFrameFull(frame);
            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    Logger.Instance.Error(ex);
                }
                if (result)
                    ReleaseFrame();
            }
            finally
            {
                try
                {
                    if (result)
                        ReleaseFrame();
                }
                catch
                {
                }
            }

            return frame;
        }

        private unsafe void ProcessFrameFull(DesktopFrame frame)
        {
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    mDevice.ImmediateContext.GenerateMips(smallerTextureView);

                    Rect sourceRect = new Rect(smallerTexture.Description.Width, smallerTexture.Description.Height);
                    Rectangle sourceRectangle = new Rectangle(0, 0, sourceRect.Width, sourceRect.Height);
                    DisplayHelper.GetSystemScale(out float scaleX, out float scaleY, out int sourceWidth, out int sourceHeight);

                    //计算出最终尺寸
                    ScalingSize(sourceRect, scaleX, scaleY, out Rect distRect);

                    //复制一份画布尺寸，用于画布复制
                    Rect textureRect = distRect;
                    //由于画布只能缩小2次方尺寸，需要计算一下
                    int sourceSubresource = ResizeRect(sourceRect,ref textureRect, sourceRectangle);
                    //拷贝画布,原始画布按2的次方缩小到小画布
                    Texture2DDescription desc = CopyTexture(textureRect, sourceRectangle, sourceSubresource, out Texture2D texture1);
                    using Texture2D texture = texture1;

                    //拷贝到图像
                    using Bitmap image = CopyImage(texture, desc);
                    //弥补尺寸，2的次方缩小很快，但是不一定能直接到最终尺寸，弥补一下
                    using Bitmap bmp = FinalSize(image, distRect, desc);

                    //画鼠标
                    using Graphics g = Graphics.FromImage(bmp);
                    CursorHelper.DrawCursorIcon(g, bmp.Width * 1.0f / sourceRect.Width, bmp.Height * 1.0f / sourceRect.Height);

                    /*
                    try
                    {
                        SendFrame(bmp);
                    }
                    catch (Exception ex)
                    {
                        if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        {
                            Logger.Instance.Error(ex);
                        }
                    }
                    */

                    //转字节数组
                    ToBytes(frame, bmp);
                }
                catch (Exception ex)
                {
                    needInit = true;
                    if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        Logger.Instance.Error(ex);
                    }
                }
            }
        }


        private void InitEncoder()
        {
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    try
                    {
                        if (h264VideoStreamEncoder != null)
                        {
                            byte[] bytes = h264VideoStreamEncoder.ReceivePacket();
                            if (bytes.Length > 0)
                            {
                                Console.WriteLine($"encode {bytes.Length}");
                                continue;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        {
                            Logger.Instance.Error(ex);
                        }
                    }
                    await Task.Delay(15);
                }

            }, TaskCreationOptions.LongRunning);
        }

        VideoFrameConverter videoFrameConverter;
        H264VideoStreamEncoder h264VideoStreamEncoder;
        private unsafe void SendFrame(Bitmap image)
        {
            if (h264VideoStreamEncoder == null)
            {
                Size sourceSize = new Size(image.Width, image.Height);
                FFmpeg.AutoGen.Abstractions.AVPixelFormat sourcePixelFormat = FFmpeg.AutoGen.Abstractions.AVPixelFormat.@AV_PIX_FMT_BGRA;
                Size destinationSize = sourceSize;
                FFmpeg.AutoGen.Abstractions.AVPixelFormat destinationPixelFormat = FFmpeg.AutoGen.Abstractions.AVPixelFormat.AV_PIX_FMT_YUV420P;
                videoFrameConverter = new VideoFrameConverter(sourceSize, sourcePixelFormat, destinationSize, destinationPixelFormat);
                h264VideoStreamEncoder = new H264VideoStreamEncoder(30, destinationSize);
            }

            byte[] sourceBitmapData = default;
            BitmapData bitmapData = image.LockBits(new System.Drawing.Rectangle(Point.Empty, image.Size), ImageLockMode.ReadOnly, image.PixelFormat);
            try
            {
                int length = bitmapData.Stride * bitmapData.Height;
                sourceBitmapData = new byte[length];
                Marshal.Copy(bitmapData.Scan0, sourceBitmapData, 0, length);
            }
            finally
            {
                image.UnlockBits(bitmapData);
            }

            fixed (byte* pBitmapData = sourceBitmapData)
            {
                var data = new byte_ptr8 { [0] = pBitmapData };
                var linesize = new int8 { [0] = sourceBitmapData.Length / image.Height };
                var avframe = new FFmpeg.AutoGen.Abstractions.AVFrame
                {
                    data = data,
                    linesize = linesize,
                    height = image.Height
                };
                FFmpeg.AutoGen.Abstractions.AVFrame convertedFrame = videoFrameConverter.Convert(avframe);
                h264VideoStreamEncoder.SendFrame(convertedFrame);

            }
        }


        private void ToBytes(DesktopFrame frame, System.Drawing.Image image)
        {
            using MemoryStream ms = new MemoryStream();
            image.Save(ms, ImageFormat.Jpeg);
            ms.Seek(0, SeekOrigin.Begin);

            int length = (int)ms.Length;
            if (length > fullImageBytes.Length)
            {
                fullImageBytes = new byte[length];
            }
            //Console.WriteLine($"image {length}");
            ms.Read(fullImageBytes.AsSpan(0, length));
            frame.FullImage = fullImageBytes.AsMemory(0, length);
        }
        private Bitmap FinalSize(Bitmap image, Rect distRect, Texture2DDescription desc)
        {
            Bitmap bmp = image;
            if (desc.Width - distRect.Width > 50)
            {
                bmp = new Bitmap(distRect.Width, distRect.Height);
                using Graphics graphic = Graphics.FromImage(bmp);
                graphic.DrawImage(image, new System.Drawing.Rectangle(0, 0, distRect.Width, distRect.Height), 0, 0, desc.Width, desc.Height, GraphicsUnit.Pixel);
            }
            return bmp;
        }
        private Bitmap CopyImage(Texture2D texture, Texture2DDescription desc)
        {
            DataBox mapSource = mDevice.ImmediateContext.MapSubresource(texture, 0, MapMode.Read, MapFlags.None);
            Bitmap image = new Bitmap(desc.Width, desc.Height, PixelFormat.Format32bppArgb);
            System.Drawing.Rectangle boundsRect = new System.Drawing.Rectangle(0, 0, desc.Width, desc.Height);
            BitmapData mapDest = image.LockBits(boundsRect, ImageLockMode.WriteOnly, image.PixelFormat);
            nint sourcePtr = mapSource.DataPointer;
            nint destPtr = mapDest.Scan0;
            for (int y = 0; y < desc.Height; y++)
            {
                Utilities.CopyMemory(destPtr, sourcePtr, desc.Width * 4);
                sourcePtr = IntPtr.Add(sourcePtr, mapSource.RowPitch);
                destPtr = IntPtr.Add(destPtr, mapDest.Stride);
            }
            image.UnlockBits(mapDest);
            mDevice.ImmediateContext.UnmapSubresource(texture, 0);

            return image;
        }
        private Texture2DDescription CopyTexture(Rect textureRect, Rectangle sourceRectangle, int sourceSubresource, out Texture2D texture)
        {
            Texture2DDescription desc = desktopImageTexture.Description;
            desc.Width = textureRect.Width;
            desc.Height = textureRect.Height;
            texture = new Texture2D(mDevice, desc);
            mDevice.ImmediateContext.CopySubresourceRegion(smallerTexture, sourceSubresource, new ResourceRegion
            {
                Left = sourceRectangle.X,
                Right = sourceRectangle.X + sourceRectangle.Width,
                Top = sourceRectangle.Y,
                Bottom = sourceRectangle.Y + sourceRectangle.Height,
                Back = 1
            }, texture, 0, 0, 0);

            return desc;
        }
        private int ResizeRect(Rect sourceRect,ref Rect textureRect, Rectangle sourceRectangle)
        {
            int sourceSubresource = sourceRectangle.Width / textureRect.Width;
            if (sourceSubresource >= 0)
            {
                while (sourceSubresource > 0 && sourceRect.Width / (1 << sourceSubresource) < textureRect.Width)
                {
                    sourceSubresource--;
                }
                if (sourceSubresource >= 3) sourceSubresource = 3;

                textureRect.Width = sourceRect.Width / (1 << sourceSubresource);
                textureRect.Height = sourceRect.Height / (1 << sourceSubresource);
            }
            return sourceSubresource;
        }

        /// <summary>
        /// 缩放尺寸
        /// </summary>
        /// <param name="sourceRect">原尺寸</param>
        /// <param name="scaleX">显示缩放</param>
        /// <param name="scaleY">显示缩放</param>
        /// <param name="rect">目标尺寸</param>
        /// <returns></returns>
        private bool ScalingSize(Rect sourceRect, float scaleX, float scaleY, out Rect rect)
        {
            int width = (int)(sourceRect.Width * 1.0 / scaleX * config.ScreenScale);
            int height = (int)(sourceRect.Height * 1.0 / scaleY * config.ScreenScale);
            rect = new Rect(width, height);
            return true;
        }


        public DesktopFrame GetLatestRegionFrame()
        {
            DesktopFrame frame = new DesktopFrame() { FullImage = Helper.EmptyArray, RegionImage = Helper.EmptyArray };
            bool success = RetrieveFrame();
            if (success == false)
            {
                return frame;
            }

            try
            {
                RetrieveFrameMetadata(frame);

                if (frame.UpdatedRegions.Length > 0)
                {
                    frame.UpdatedRegions = Rectangle.UnionRectangles(frame.UpdatedRegions);
                    ProcessFrameRegion(frame);
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
        byte[] regionImageBytes = Helper.EmptyArray;
        private unsafe void ProcessFrameRegion(DesktopFrame frame)
        {
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    /*
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
                    */
                    Rectangle[] updatedRegions = frame.UpdatedRegions;

                    int index = 0;
                    for (int i = 0; i < updatedRegions.Length; i++)
                    {
                        Rectangle region = updatedRegions[i];

                        //拷贝画布
                        Texture2DDescription desc = desktopImageTexture.Description;
                        desc.Width = region.Width;
                        desc.Height = region.Height;
                        using Texture2D texture = new Texture2D(mDevice, desc);
                        mDevice.ImmediateContext.CopySubresourceRegion(smallerTexture, 0, new ResourceRegion
                        {
                            Left = region.X,
                            Right = region.X + region.Width,
                            Top = region.Y,
                            Bottom = region.Y + region.Height,
                            Back = 1
                        }, texture, 0, 0, 0);

                        //拷贝到图像
                        DataBox mapSource = mDevice.ImmediateContext.MapSubresource(texture, 0, MapMode.Read, MapFlags.None);
                        using Bitmap image = new Bitmap(desc.Width, desc.Height, PixelFormat.Format32bppArgb);
                        System.Drawing.Rectangle boundsRect = new System.Drawing.Rectangle(0, 0, desc.Width, desc.Height);
                        BitmapData mapDest = image.LockBits(boundsRect, ImageLockMode.WriteOnly, image.PixelFormat);
                        nint sourcePtr = mapSource.DataPointer;
                        nint destPtr = mapDest.Scan0;
                        for (int y = 0; y < desc.Height; y++)
                        {
                            Utilities.CopyMemory(destPtr, sourcePtr, desc.Width * 4);
                            sourcePtr = IntPtr.Add(sourcePtr, mapSource.RowPitch);
                            destPtr = IntPtr.Add(destPtr, mapDest.Stride);
                        }
                        image.UnlockBits(mapDest);
                        mDevice.ImmediateContext.UnmapSubresource(texture, 0);


                        int width = (int)(region.Width * config.ScreenScale);
                        int height = (int)(region.Height * config.ScreenScale);
                        if (width <= 0 || height <= 0) continue;

                        using Bitmap bmp = new Bitmap(width, height);
                        using Graphics graphic = Graphics.FromImage(bmp);
                        graphic.DrawImage(image, new System.Drawing.Rectangle(0, 0, width, height), 0, 0, desc.Width, desc.Height, GraphicsUnit.Pixel);


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


    }

}
