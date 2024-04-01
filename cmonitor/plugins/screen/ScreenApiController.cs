using cmonitor.api;
using cmonitor.config;
using cmonitor.plugins.screen.messenger;
using cmonitor.plugins.screen.report;
using cmonitor.plugins.signIn.messenger;
using cmonitor.server;
using common.libs.extends;
using MemoryPack;

namespace cmonitor.plugins.screen
{
    public sealed class ScreenApiController : IApiController
    {
        private readonly MessengerSender messengerSender;
        private readonly SignCaching signCaching;
        private readonly Config config;
        public ScreenApiController(MessengerSender messengerSender, SignCaching signCaching, Config config)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
            this.config = config;
        }
        public bool Full(ApiControllerParamsInfo param)
        {
            ScreenReportInfo report = param.Content.DeJson<ScreenReportInfo>();
            byte[] bytes = new byte[] { (byte)report.Type };
            for (int i = 0; i < report.Names.Length; i++)
            {
                bool connectionRes = signCaching.Get(report.Names[i], out SignCacheInfo cache) && cache.Connected;
                if (connectionRes == false) continue;
                bool reportRes = cache.GetScreen(200) && Interlocked.CompareExchange(ref cache.ScreenFlag, 0, 1) == 1;

                if (connectionRes && reportRes)
                {
                    cache.UpdateScreen();
                    _ = messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = cache.Connection,
                        MessengerId = (ushort)ScreenMessengerIds.CaptureFull,
                        Timeout = 1000,
                        Payload = bytes
                    }).ContinueWith((result) =>
                    {
                        Interlocked.Exchange(ref cache.ScreenFlag, 1);
                    });
                }
            }

            return true;
        }
        public bool Clip(ApiControllerParamsInfo param)
        {
            ScreenClipParamInfo screenClipParamInfo = param.Content.DeJson<ScreenClipParamInfo>();
            if (signCaching.Get(screenClipParamInfo.Name, out SignCacheInfo cache))
            {
                _ = messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    MessengerId = (ushort)ScreenMessengerIds.CaptureClip,
                    Timeout = 1000,
                    Payload = MemoryPackSerializer.Serialize(screenClipParamInfo.Clip)
                });
            }
            return true;
        }

        public bool Region(ApiControllerParamsInfo param)
        {
            string[] names = param.Content.DeJson<string[]>();
            for (int i = 0; i < names.Length; i++)
            {
                bool res = signCaching.Get(names[i], out SignCacheInfo cache)
                    && cache.Connected
                    && cache.GetScreen(200)
                    && Interlocked.CompareExchange(ref cache.ScreenFlag, 0, 1) == 1;
                if (res)
                {
                    cache.UpdateScreen();
                    _ = messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = cache.Connection,
                        MessengerId = (ushort)ScreenMessengerIds.CaptureRegion,
                        Timeout = 1000,
                    }).ContinueWith((result) =>
                    {
                        Interlocked.Exchange(ref cache.ScreenFlag, 1);
                    });
                }
            }

            return true;
        }


    }

    public sealed class ScreenShareParamInfo
    {
        public string Name { get; set; }
        public string[] Names { get; set; }
    }

    public sealed class ScreenReportInfo
    {
        public string[] Names { get; set; }
        public ScreenReportFullType Type { get; set; }
    }
    public sealed class ScreenClipParamInfo
    {
        public string Name { get; set; }
        public ScreenClipInfo Clip { get; set; }
    }
}
