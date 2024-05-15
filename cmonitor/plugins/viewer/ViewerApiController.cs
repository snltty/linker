using cmonitor.plugins.signin.messenger;
using cmonitor.plugins.viewer.messenger;
using cmonitor.server;
using common.libs.extends;
using MemoryPack;
using common.libs.api;
using cmonitor.plugins.viewer.config;
using cmonitor.server.sapi;

namespace cmonitor.plugins.viewer
{
    public sealed class ViewerApiController : IApiServerController
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
            //去掉服务端,
            var list = viewer.Clients.ToList();
            list.Remove(viewer.Server);
            viewer.Clients = list.ToArray();

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
        }
    }

}
