using cmonitor.api;
using cmonitor.plugins.signIn.messenger;
using cmonitor.plugins.viewer.messenger;
using cmonitor.plugins.viewer.report;
using cmonitor.server;
using common.libs.extends;
using MemoryPack;

namespace cmonitor.plugins.viewer
{
    public sealed class ViewerApiController : IApiController
    {
        private readonly MessengerSender messengerSender;
        private readonly SignCaching signCaching;
        public ViewerApiController(MessengerSender messengerSender, SignCaching signCaching)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
        }
        public bool Update(ApiControllerParamsInfo param)
        {
            ViewerUpdateParamInfo viewer = param.Content.DeJson<ViewerUpdateParamInfo>();
            //去掉服务端
            var list = viewer.Clients.ToList();
            list.Remove(viewer.Server);
            viewer.Clients = list.ToArray();

            if (signCaching.Get(viewer.Server, out SignCacheInfo cache) && cache.Connected)
            {
                byte[] serverBytes = MemoryPackSerializer.Serialize(new ViewerConfigInfo
                {
                    Clients = viewer.Clients,
                    ConnectStr = string.Empty,
                    Mode = ViewerMode.Server,
                    Open = viewer.Open
                });
                _ = messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    MessengerId = (ushort)ViewerMessengerIds.Server,
                    Payload = serverBytes
                });
            }
            return true;
        }
        public sealed class ViewerUpdateParamInfo
        {
            public bool Open { get; set; }
            public string Server { get; set; } = string.Empty;
            public string[] Clients { get; set; } = Array.Empty<string>();
        }
    }

}
