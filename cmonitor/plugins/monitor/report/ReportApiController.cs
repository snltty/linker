using cmonitor.client.report;
using cmonitor.config;
using cmonitor.plugins.report.messenger;
using cmonitor.plugins.signin.messenger;
using cmonitor.server;
using common.libs;
using common.libs.extends;
using common.libs.api;
using cmonitor.server.sapi;

namespace cmonitor.plugins.report
{
    public sealed class ReportApiController : IApiServerController
    {
        private readonly MessengerSender messengerSender;
        private readonly SignCaching signCaching;
        private readonly IApiServerServer clientServer;
        private readonly Config config;
        private readonly FpsHelper fpsHelper = new FpsHelper();

        public ReportApiController(MessengerSender messengerSender, SignCaching signCaching, IApiServerServer clientServer, Config config)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
            this.clientServer = clientServer;
            this.config = config;
        }
        public bool Update(ApiControllerParamsInfo param)
        {
            UpdateInfo updateinfo = param.Content.DeJson<UpdateInfo>();
            byte[] bytes = new byte[] { (byte)updateinfo.ReportType };
            for (int i = 0; i < updateinfo.Names.Length; i++)
            {
                string name = updateinfo.Names[i];
                bool connectionRes = signCaching.TryGet(name, out SignCacheInfo cache) && cache.Connected;
                if (connectionRes == false) continue;
                bool reportTimeRes = fpsHelper.Acquire(name, 30);

                if (connectionRes && (reportTimeRes || updateinfo.ReportType == ReportType.Full))
                {
                    _ = messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = cache.Connection,
                        MessengerId = (ushort)ReportMessengerIds.Update,
                        Timeout = 1000,
                        Payload = bytes,
                    }).ContinueWith((result) =>
                    {
                        fpsHelper.Release(name);
                    });
                }
            }
            return true;
        }


        public bool Ping(ApiControllerParamsInfo param)
        {
            string[] names = param.Content.DeJson<string[]>();
            for (int i = 0; i < names.Length; i++)
            {
                string name = names[i];
                if (signCaching.TryGet(names[i], out SignCacheInfo cache) && cache.Connected && fpsHelper.Acquire(name, 30))
                {
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
                            clientServer.Notify("/notify/report/pong", new { Name = name, Time = (int)(DateTime.Now - starTime).TotalMilliseconds }, param.Connection);
                        }
                        else
                        {
                            clientServer.Notify("/notify/report/pong", new { Name = name, Time = -1 }, param.Connection);
                        }
                        fpsHelper.Release(name);
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