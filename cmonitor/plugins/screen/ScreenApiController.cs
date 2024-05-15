using cmonitor.config;
using cmonitor.plugins.screen.messenger;
using cmonitor.plugins.screen.report;
using cmonitor.plugins.signin.messenger;
using cmonitor.server;
using common.libs;
using common.libs.extends;
using MemoryPack;
using common.libs.api;
using cmonitor.server.sapi;

namespace cmonitor.plugins.screen
{
    public sealed class ScreenApiController : IApiServerController
    {
        private readonly MessengerSender messengerSender;
        private readonly SignCaching signCaching;
        private readonly Config config;
        private readonly FpsHelper fpsHelper = new FpsHelper();

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
                string name = report.Names[i];
                if (signCaching.Get(name, out SignCacheInfo cache) && cache.Connected && fpsHelper.Acquire(name, 5))
                {
                    _ = messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = cache.Connection,
                        MessengerId = (ushort)ScreenMessengerIds.CaptureFull,
                        Timeout = 1000,
                        Payload = bytes
                    }).ContinueWith((result) =>
                    {
                        fpsHelper.Release(name);
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
                string name = names[i];
                if (signCaching.Get(names[i], out SignCacheInfo cache) && cache.Connected && fpsHelper.Acquire(name, 5))
                {
                    _ = messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = cache.Connection,
                        MessengerId = (ushort)ScreenMessengerIds.CaptureRegion,
                        Timeout = 1000,
                    }).ContinueWith((result) =>
                    {
                        fpsHelper.Release(name);
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
