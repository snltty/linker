using FFmpeg.AutoGen;
using System.Drawing;

namespace cmonitor.client.reports.screen.h264
{
    public unsafe class H264VideoStreamDecoder
    {
        private readonly AVCodecContext* _pCodecContext;
        private readonly AVFrame* _pFrame;
        private readonly AVPacket* _pPacket;

        public H264VideoStreamDecoder(int fps, Size frameSize, string[] args)
        {
            var codecId = AVCodecID.AV_CODEC_ID_H264;
            var pCodec = ffmpeg.avcodec_find_decoder(codecId);
            if (pCodec == null) throw new InvalidOperationException("Codec not found.");

            _pCodecContext = ffmpeg.avcodec_alloc_context3(pCodec);
            _pCodecContext->width = frameSize.Width;
            _pCodecContext->height = frameSize.Height;
            _pCodecContext->time_base = new AVRational { num = 1, den = fps };
            _pCodecContext->pix_fmt = AVPixelFormat.AV_PIX_FMT_YUV420P;
            foreach (var arg in args)
            {
                var optionName = arg.Split('=')[0];
                var value = arg.Split('=')[1];
                ffmpeg.av_opt_set(_pCodecContext->priv_data, optionName, value, 0);
            }
            //ffmpeg.av_opt_set(_pCodecContext->priv_data, "preset", "veryfast", 0);
            //ffmpeg.av_opt_set(_pCodecContext->priv_data, "tune", "zerolatency", 0);
            ffmpeg.avcodec_open2(_pCodecContext, pCodec, null).ThrowExceptionIfError();
        }

        public AVFrame Decoder(AVPacket packet)
        {
            var frame = ffmpeg.av_frame_alloc();
            int error;
            do
            {
                ffmpeg.avcodec_send_packet(_pCodecContext, &packet).ThrowExceptionIfError();
                error = ffmpeg.avcodec_receive_frame(_pCodecContext, frame);
            } while (error == ffmpeg.AVERROR(ffmpeg.EAGAIN));

            ffmpeg.av_packet_unref(&packet);

            return *frame;
        }
    }
}
