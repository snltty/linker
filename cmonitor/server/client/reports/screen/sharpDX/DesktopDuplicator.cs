using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using SharpDX.Mathematics.Interop;
using System.Buffers;
using Rectangle = System.Drawing.Rectangle;
using Point = System.Drawing.Point;
using cmonitor.server.client.reports.screen.aforge;
using common.libs;
using SharpDX.Multimedia;

namespace cmonitor.server.client.reports.screen.sharpDX
{
    public class DesktopDuplicator
    {
        private Device mDevice;
        private Texture2DDescription mTextureDesc;
        private OutputDescription mOutputDesc;
        private OutputDuplication mDeskDupl;

        private Texture2D desktopImageTexture = null;
        private OutputDuplicateFrameInformation frameInfo = new OutputDuplicateFrameInformation();
        private int mWhichOutputDevice = -1;

        public DesktopDuplicator(int whichMonitor)
            : this(0, whichMonitor) { }

        public DesktopDuplicator(int whichGraphicsCardAdapter, int whichOutputDevice)
        {
            this.mWhichOutputDevice = whichOutputDevice;
            Adapter1 adapter = null;
            try
            {
                adapter = new Factory1().GetAdapter1(whichGraphicsCardAdapter);
            }
            catch (SharpDXException)
            {
                throw new DesktopDuplicationException("Could not find the specified graphics card adapter.");
            }
            this.mDevice = new Device(adapter);
            Output output = null;
            try
            {
                output = adapter.GetOutput(whichOutputDevice);
            }
            catch (SharpDXException)
            {
                throw new DesktopDuplicationException("Could not find the specified output device.");
            }
            var output1 = output.QueryInterface<Output1>();
            this.mOutputDesc = output.Description;
            this.mTextureDesc = new Texture2DDescription()
            {
                CpuAccessFlags = CpuAccessFlags.Read,
                BindFlags = BindFlags.None,
                Format = Format.B8G8R8A8_UNorm,
                Width = this.mOutputDesc.DesktopBounds.Right - this.mOutputDesc.DesktopBounds.Left,
                Height = this.mOutputDesc.DesktopBounds.Bottom - this.mOutputDesc.DesktopBounds.Top,
                OptionFlags = ResourceOptionFlags.None,
                MipLevels = 1,
                ArraySize = 1,
                SampleDescription = { Count = 1, Quality = 0 },
                Usage = ResourceUsage.Staging
            };

            try
            {
                this.mDeskDupl = output1.DuplicateOutput(mDevice);
            }
            catch (SharpDXException ex)
            {
                Logger.Instance.Error(ex);
                if (ex.ResultCode.Code == SharpDX.DXGI.ResultCode.NotCurrentlyAvailable.Result.Code)
                {
                    throw new DesktopDuplicationException("There is already the maximum number of applications using the Desktop Duplication API running, please close one of the applications and try again.");
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
        }

        public DesktopFrame GetLatestFrame(float configScale, out int length)
        {
            length = 0;
            DesktopFrame frame = new DesktopFrame() { DesktopImage = Helper.EmptyArray };
            bool retrievalTimedOut = RetrieveFrame();
            if (retrievalTimedOut)
                return null;
            try
            {
                RetrieveFrameMetadata(frame);
                //RetrieveCursorMetadata(frame);
                ProcessFrame(frame, configScale, out length);
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

        private bool RetrieveFrame()
        {
            if (desktopImageTexture == null)
                desktopImageTexture = new Texture2D(mDevice, mTextureDesc);
            SharpDX.DXGI.Resource desktopResource = null;
            frameInfo = new OutputDuplicateFrameInformation();

            try
            {
                mDeskDupl.TryAcquireNextFrame(500, out frameInfo, out desktopResource);
            }
            catch (SharpDXException ex)
            {
                if (ex.ResultCode.Code == SharpDX.DXGI.ResultCode.WaitTimeout.Result.Code)
                {
                    return true;
                }
                if (ex.ResultCode.Failure)
                {
                    throw new DesktopDuplicationException("Failed to acquire next frame.");
                }
            }
            using (var tempTexture = desktopResource.QueryInterface<Texture2D>())
                mDevice.ImmediateContext.CopyResource(tempTexture, desktopImageTexture);
            desktopResource.Dispose();
            return false;
        }

        private void RetrieveFrameMetadata(DesktopFrame frame)
        {

            if (frameInfo.TotalMetadataBufferSize > 0)
            {
                int movedRegionsLength = 0;
                OutputDuplicateMoveRectangle[] movedRectangles = new OutputDuplicateMoveRectangle[frameInfo.TotalMetadataBufferSize];
                mDeskDupl.GetFrameMoveRects(movedRectangles.Length, movedRectangles, out movedRegionsLength);
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

                int dirtyRegionsLength = 0;
                RawRectangle[] dirtyRectangles = new RawRectangle[frameInfo.TotalMetadataBufferSize];
                mDeskDupl.GetFrameDirtyRects(dirtyRectangles.Length, dirtyRectangles, out dirtyRegionsLength);
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
                frame.UpdatedRegions = new System.Drawing.Rectangle[0];
            }
        }
        private void RetrieveCursorMetadata(DesktopFrame frame)
        {
            var pointerInfo = new PointerInfo();

            if (frameInfo.LastMouseUpdateTime == 0)
                return;

            bool updatePosition = true;

            if (!frameInfo.PointerPosition.Visible && (pointerInfo.WhoUpdatedPositionLast != this.mWhichOutputDevice))
                updatePosition = false;

            if (frameInfo.PointerPosition.Visible && pointerInfo.Visible && (pointerInfo.WhoUpdatedPositionLast != this.mWhichOutputDevice) && (pointerInfo.LastTimeStamp > frameInfo.LastMouseUpdateTime))
                updatePosition = false;

            if (updatePosition)
            {
                pointerInfo.Position = new Point(frameInfo.PointerPosition.Position.X, frameInfo.PointerPosition.Position.Y);
                pointerInfo.WhoUpdatedPositionLast = mWhichOutputDevice;
                pointerInfo.LastTimeStamp = frameInfo.LastMouseUpdateTime;
                pointerInfo.Visible = frameInfo.PointerPosition.Visible;
            }

            if (frameInfo.PointerShapeBufferSize == 0)
                return;

            if (frameInfo.PointerShapeBufferSize > pointerInfo.BufferSize)
            {
                pointerInfo.PtrShapeBuffer = new byte[frameInfo.PointerShapeBufferSize];
                pointerInfo.BufferSize = frameInfo.PointerShapeBufferSize;
            }

            try
            {
                unsafe
                {
                    fixed (byte* ptrShapeBufferPtr = pointerInfo.PtrShapeBuffer)
                    {
                        mDeskDupl.GetFramePointerShape(frameInfo.PointerShapeBufferSize, (IntPtr)ptrShapeBufferPtr, out pointerInfo.BufferSize, out pointerInfo.ShapeInfo);
                    }
                }
            }
            catch (SharpDXException ex)
            {
                if (ex.ResultCode.Failure)
                {
                    throw new DesktopDuplicationException("Failed to get frame pointer shape.");
                }
            }

            //frame.CursorLocation = new System.Drawing.Point(pointerInfo.Position.X, pointerInfo.Position.Y);
        }

        ResizeBilinear resizeFilter;
        private unsafe void ProcessFrame(DesktopFrame frame, float configScale, out int length)
        {
            length = 0;
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    var mapSource = mDevice.ImmediateContext.MapSubresource(desktopImageTexture, 0, MapMode.Read, MapFlags.None);
                    int sourceWidth = mOutputDesc.DesktopBounds.Right - mOutputDesc.DesktopBounds.Left;
                    int sourceHeight = mOutputDesc.DesktopBounds.Bottom - mOutputDesc.DesktopBounds.Top;

                    GdiCapture.GetScale(out int scalex, out int scaley, out int sourceWidth1, out int sourceHeight1);
                    GdiCapture.GetNewSize(sourceWidth, sourceHeight, scalex, scaley, configScale, out int width, out int height);


                    using Bitmap image = new Bitmap(sourceWidth, sourceHeight, PixelFormat.Format32bppRgb);
                    Rectangle boundsRect = new Rectangle(0, 0, sourceWidth, sourceHeight);
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

                    //using Graphics g = Graphics.FromImage(image);
                    //GdiCapture.DrawCursorIcon(g, sourceWidth, scalex, scaley, configScale);

                    if (resizeFilter == null)
                    {
                        resizeFilter = new ResizeBilinear(width, height);
                    }
                    Bitmap bmp = resizeFilter.Apply(image);



                    using System.Drawing.Image image1 = bmp;
                    using MemoryStream ms = new MemoryStream();
                    image1.Save(ms, ImageFormat.Jpeg);
                    ms.Seek(0, SeekOrigin.Begin);

                    length = (int)ms.Length;

                    frame.DesktopImage = ArrayPool<byte>.Shared.Rent((int)ms.Length);
                    ms.Read(frame.DesktopImage);
                }
                catch (Exception)
                {
                }
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
                    throw new DesktopDuplicationException("Failed to release frame.");
                }
            }
        }
    }
}
