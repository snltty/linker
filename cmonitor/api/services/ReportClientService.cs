using cmonitor.client.reports;
using cmonitor.service;
using cmonitor.service.messengers.command;
using cmonitor.service.messengers.sign;
using common.libs;
using common.libs.extends;
using System.Diagnostics;

namespace cmonitor.api.services
{
    public sealed class ReportClientService : IClientService
    {
        private readonly MessengerSender messengerSender;
        private readonly SignCaching signCaching;
        private readonly IClientServer clientServer;
        private readonly Config config;
        public ReportClientService(MessengerSender messengerSender, SignCaching signCaching, IClientServer clientServer, Config config)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
            this.clientServer = clientServer;
            this.config = config;
        }
        public bool Update(ClientServiceParamsInfo param)
        {
            UpdateInfo updateinfo = param.Content.DeJson<UpdateInfo>();
            byte[] bytes = new byte[] { (byte)updateinfo.ReportType };
            for (int i = 0; i < updateinfo.Names.Length; i++)
            {
                bool connectionRes = (signCaching.Get(updateinfo.Names[i], out SignCacheInfo cache) && cache.Connected);
                if (connectionRes == false) continue;
                bool reportTimeRes = cache.GetReport(config.ReportDelay) && Interlocked.CompareExchange(ref cache.ReportFlag, 0, 1) == 1;

                if (connectionRes && (reportTimeRes || updateinfo.ReportType == ReportType.Full))
                {
                    cache.UpdateReport();
                    _ = messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = cache.Connection,
                        MessengerId = (ushort)ReportMessengerIds.Update,
                        Timeout = 1000,
                        Payload = bytes,
                    }).ContinueWith((result) =>
                    {
                        Interlocked.Exchange(ref cache.ReportFlag, 1);
                    });
                }
            }
            return true;
        }


        public bool Ping(ClientServiceParamsInfo param)
        {
            string[] names = param.Content.DeJson<string[]>();
            for (int i = 0; i < names.Length; i++)
            {
                if (signCaching.Get(names[i], out SignCacheInfo cache) && cache.Connected && Interlocked.CompareExchange(ref cache.PingFlag, 0, 1) == 1)
                {
                    string name = names[i];
                    DateTime starTime = DateTime.Now;
                    _ = messengerSender.SendReply(new MessageRequestWrap
                    {
                        Connection = cache.Connection,
                        MessengerId = (ushort)ReportMessengerIds.Ping,
                        Payload = Helper.EmptyArray,
                        Timeout = 1000,
                    }).ContinueWith((result) =>
                    {
                        if (result.Result.Code == MessageResponeCodes.OK)
                        {
                            clientServer.Notify("/notify/report/pong", new { Name = name, Time = (int)((DateTime.Now - starTime).TotalMilliseconds) }, param.Connection);
                        }
                        else
                        {
                            clientServer.Notify("/notify/report/pong", new { Name = name, Time = -1 }, param.Connection);
                        }
                        Interlocked.Exchange(ref cache.PingFlag, 1);
                    });
                }
            }

            return true;
        }

        sealed class UpdateInfo
        {
            public string[] Names { get; set; }
            public ReportType ReportType { get; set; }
        }
    }

}