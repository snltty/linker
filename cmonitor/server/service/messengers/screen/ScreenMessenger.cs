using cmonitor.server.api;
using cmonitor.server.client.reports.screen;
using cmonitor.server.service.messengers.sign;
using MemoryPack;
using MemoryPack.Compression;
using System;

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

        [MessengerId((ushort)ScreenMessengerIds.Update)]
        public void Update(IConnection connection)
        {
            screenReport.Update();
        }

        [MessengerId((ushort)ScreenMessengerIds.Report)]
        public void Report(IConnection connection)
        {
            if (signCaching.Get(connection.Name, out SignCacheInfo cache))
            {
                if (cache.Version == config.Version)
                {
                    clientServer.Notify("/notify/report/screen", connection.Name, connection.ReceiveRequestWrap.Payload);
                    //clientServer.Notify("/notify/report/screen", new { connection.Name, Img = connection.ReceiveRequestWrap.Payload.ToArray() });
                }
                else
                {
                    string base64 = MemoryPackSerializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
                    clientServer.Notify("/notify/report/screen", new { connection.Name, Img = base64 });
                }
            }
        }
    }

}
