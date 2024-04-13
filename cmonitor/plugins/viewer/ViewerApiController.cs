using cmonitor.api;
using cmonitor.plugins.signIn.messenger;
using cmonitor.plugins.viewer.messenger;
using cmonitor.plugins.viewer.proxy;
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
        private readonly ViewerProxyCaching viewerProxyCaching;

        public ViewerApiController(MessengerSender messengerSender, SignCaching signCaching, ViewerProxyCaching viewerProxyCaching)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
            this.viewerProxyCaching = viewerProxyCaching;
        }
        public bool Update(ApiControllerParamsInfo param)
        {
            ViewerUpdateParamInfo viewer = param.Content.DeJson<ViewerUpdateParamInfo>();
            //去掉服务端,
            var list = viewer.Clients.ToList();
            list.Remove(viewer.Server);
            viewer.Clients = list.ToArray();
            viewerProxyCaching.Remove(viewer.ShareId);

            if (signCaching.Get(viewer.Server, out SignCacheInfo cache) && cache.Connected)
            {
                ViewerRunningConfigInfo info = new ViewerRunningConfigInfo
                {
                    ServerMachine = viewer.Server,
                    ClientMachines = viewer.Clients,
                    ConnectStr = string.Empty,
                    Mode = ViewerMode.Server,
                    Open = viewer.Open
                };
                if (info.Open)
                {
                    info.ShareId = viewerProxyCaching.Set(viewer.Server);
                }

                byte[] serverBytes = MemoryPackSerializer.Serialize(info);
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

            public string ShareId { get; set; } = string.Empty;
        }
    }

}
