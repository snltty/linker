using cmonitor.server.api;
using cmonitor.server.client.reports.screen;
using cmonitor.server.service.messengers.sign;
using MemoryPack;

namespace cmonitor.server.service.messengers.screen
{
    public sealed class ScreenMessenger : IMessenger
    {
        private readonly ScreenReport screenReport;
        private readonly IClientServer clientServer;
        private readonly Config config;
        private readonly SignCaching signCaching;

        public ScreenMessenger(ScreenReport screenReport, IClientServer clientServer, Config config, SignCaching signCaching)
        {
            this.screenReport = screenReport;
            this.clientServer = clientServer;
            this.config = config;
            this.signCaching = signCaching;
        }

        [MessengerId((ushort)ScreenMessengerIds.Full)]
        public void Full(IConnection connection)
        {
            ScreenReportFullType screenReportFullType = ScreenReportFullType.Trim;
            if(connection.ReceiveRequestWrap.Payload.Length > 0)
            {
                screenReportFullType = (ScreenReportFullType)connection.ReceiveRequestWrap.Payload.Span[0];
            }
            screenReport.Full(screenReportFullType);
        }
        [MessengerId((ushort)ScreenMessengerIds.FullReport)]
        public void FullReport(IConnection connection)
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


        [MessengerId((ushort)ScreenMessengerIds.Clip)]
        public void Clip(IConnection connection)
        {
            screenReport.Clip(MemoryPackSerializer.Deserialize<ScreenClipInfo>(connection.ReceiveRequestWrap.Payload.Span));
        }


        [MessengerId((ushort)ScreenMessengerIds.Region)]
        public void Region(IConnection connection)
        {
            screenReport.Region();
        }

        [MessengerId((ushort)ScreenMessengerIds.RegionReport)]
        public void RegionReport(IConnection connection)
        {
            clientServer.Notify("/notify/report/screen/region", connection.Name, connection.ReceiveRequestWrap.Payload);
        }

        [MessengerId((ushort)ScreenMessengerIds.Rectangles)]
        public void Rectangles(IConnection connection)
        {
            Rectangle[] rectangles = MemoryPackSerializer.Deserialize<Rectangle[]>(connection.ReceiveRequestWrap.Payload.Span);
            clientServer.Notify("/notify/report/screen/rectangles", new { Name = connection.Name, Rectangles = rectangles });
        }
    }

}
