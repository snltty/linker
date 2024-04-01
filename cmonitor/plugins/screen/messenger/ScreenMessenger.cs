using cmonitor.api;
using cmonitor.config;
using cmonitor.plugins.screen.report;
using cmonitor.plugins.signIn.messenger;
using cmonitor.server;
using MemoryPack;

namespace cmonitor.plugins.screen.messenger
{
    public sealed class ScreenClientMessenger : IMessenger
    {
        private readonly ScreenReport screenReport;


        public ScreenClientMessenger(ScreenReport screenReport)
        {
            this.screenReport = screenReport;
        }

        [MessengerId((ushort)ScreenMessengerIds.CaptureFull)]
        public void CaptureFull(IConnection connection)
        {
            ScreenReportFullType reportType = ScreenReportFullType.Trim;
            if (connection.ReceiveRequestWrap.Payload.Length > 0)
            {
                reportType = (ScreenReportFullType)connection.ReceiveRequestWrap.Payload.Span[0];
            }
            screenReport.SetCaptureFull(reportType);
        }

        [MessengerId((ushort)ScreenMessengerIds.CaptureClip)]
        public void CaptureClip(IConnection connection)
        {
            screenReport.SetCaptureClip(MemoryPackSerializer.Deserialize<ScreenClipInfo>(connection.ReceiveRequestWrap.Payload.Span));
        }

        [MessengerId((ushort)ScreenMessengerIds.CaptureRegion)]
        public void CaptureRegion(IConnection connection)
        {
            screenReport.SetCaptureRegion();
        }

    }


    public sealed class ScreenServerMessenger : IMessenger
    {
        private readonly IApiServer clientServer;
        private readonly Config config;
        private readonly SignCaching signCaching;


        public ScreenServerMessenger( IApiServer clientServer, Config config, SignCaching signCaching)
        {
            this.clientServer = clientServer;
            this.config = config;
            this.signCaching = signCaching;
        }

      
        [MessengerId((ushort)ScreenMessengerIds.CaptureFullReport)]
        public void CaptureFullReport(IConnection connection)
        {
            if (signCaching.Get(connection.Name, out SignCacheInfo cache))
            {
                if (cache.Version == config.Version)
                {
                    clientServer.Notify("/notify/report/screen/full", connection.Name, connection.ReceiveRequestWrap.Payload);
                }
                else
                {
                    string base64 = MemoryPackSerializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
                    clientServer.Notify("/notify/report/screen/full", new { connection.Name, Img = base64 });
                }
            }
        }

        [MessengerId((ushort)ScreenMessengerIds.CaptureRegionReport)]
        public void CaptureRegionReport(IConnection connection)
        {
            clientServer.Notify("/notify/report/screen/region", connection.Name, connection.ReceiveRequestWrap.Payload);
        }

        [MessengerId((ushort)ScreenMessengerIds.CaptureRectangles)]
        public void CaptureRectangles(IConnection connection)
        {
            Rectangle[] rectangles = MemoryPackSerializer.Deserialize<Rectangle[]>(connection.ReceiveRequestWrap.Payload.Span);
            clientServer.Notify("/notify/report/screen/rectangles", new { connection.Name, Rectangles = rectangles });
        }
    }
}
