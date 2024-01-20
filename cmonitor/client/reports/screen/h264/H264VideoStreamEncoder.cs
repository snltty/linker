using System.Drawing;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen.Abstractions;

namespace cmonitor.client.reports.screen.h264;

public sealed unsafe class H264VideoStreamEncoder : IDisposable
{
    private readonly Size _frameSize;
    private readonly int _linesizeU;
    private readonly int _linesizeV;
    private readonly int _linesizeY;
    private readonly AVCodec* _pCodec;
    private readonly AVCodecContext* _pCodecContext;
    private readonly int _uSize;
    private readonly int _ySize;

    public H264VideoStreamEncoder(int fps, Size frameSize)
    {
        _frameSize = frameSize;

        var codecId = AVCodecID.AV_CODEC_ID_H264;
        _pCodec = ffmpeg.avcodec_find_encoder(codecId);
        if (_pCodec == null) throw new InvalidOperationException("Codec not found.");

        _pCodecContext = ffmpeg.avcodec_alloc_context3(_pCodec);
        _pCodecContext->width = frameSize.Width;
        _pCodecContext->height = frameSize.Height;
        _pCodecContext->time_base = new AVRational { num = 1, den = fps };
        _pCodecContext->pix_fmt = AVPixelFormat.AV_PIX_FMT_YUV420P;
        ffmpeg.av_opt_set(_pCodecContext->priv_data, "preset", "superfast", 0);
        //ffmpeg.av_opt_set(_pCodecContext->priv_data, "-b:v", "50K", 0);
        //ffmpeg.av_opt_set(_pCodecContext->priv_data, "-minrate", "50K", 0);
        //ffmpeg.av_opt_set(_pCodecContext->priv_data, "-maxrate", "5000K", 0);
        //ffmpeg.av_opt_set(_pCodecContext->priv_data, "-bufsize", "5000K", 0);

        ffmpeg.avcodec_open2(_pCodecContext, _pCodec, null).ThrowExceptionIfError();

        _linesizeY = frameSize.Width;
        _linesizeU = frameSize.Width / 2;
        _linesizeV = frameSize.Width / 2;

        _ySize = _linesizeY * frameSize.Height;
        _uSize = _linesizeU * frameSize.Height / 2;
    }

    public void Dispose()
    {
        ffmpeg.avcodec_close(_pCodecContext);
        ffmpeg.av_free(_pCodecContext);
        ffmpeg.av_free(_pCodec);
    }

    public void SendFrame(AVFrame frame)
    {
        if (frame.format != (int)_pCodecContext->pix_fmt)
            throw new ArgumentException("Invalid pixel format.", nameof(frame));
        if (frame.width != _frameSize.Width) throw new ArgumentException("Invalid width.", nameof(frame));
        if (frame.height != _frameSize.Height) throw new ArgumentException("Invalid height.", nameof(frame));
        if (frame.linesize[0] < _linesizeY) throw new ArgumentException("Invalid Y linesize.", nameof(frame));
        if (frame.linesize[1] < _linesizeU) throw new ArgumentException("Invalid U linesize.", nameof(frame));
        if (frame.linesize[2] < _linesizeV) throw new ArgumentException("Invalid V linesize.", nameof(frame));
        if (frame.data[1] - frame.data[0] < _ySize)
            throw new ArgumentException("Invalid Y data size.", nameof(frame));
        if (frame.data[2] - frame.data[1] < _uSize)
            throw new ArgumentException("Invalid U data size.", nameof(frame));

        try
        {
            ffmpeg.avcodec_send_frame(_pCodecContext, &frame).ThrowExceptionIfError();
        }
        catch (Exception)
        {
        }
        finally
        {
            ffmpeg.av_frame_unref(&frame);
        }


    }

    public byte[] ReceivePacket()
    {
        AVPacket* pPacket = ffmpeg.av_packet_alloc();

        try
        {
            ffmpeg.av_packet_unref(pPacket);
            int response = ffmpeg.avcodec_receive_packet(_pCodecContext, pPacket);
            if(response == 0)
            {
                byte[] bytes = new byte[pPacket->size];
                Marshal.Copy((IntPtr)pPacket->data, bytes, 0, pPacket->size);
                return bytes;
            }
        }
        finally
        {

            ffmpeg.av_packet_free(&pPacket);
        }

        return Array.Empty<byte>();
    }
}
