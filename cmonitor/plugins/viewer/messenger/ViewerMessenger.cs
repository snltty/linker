using cmonitor.plugins.signin.messenger;
using cmonitor.plugins.viewer.config;
using cmonitor.plugins.viewer.report;
using cmonitor.server;
using MemoryPack;

namespace cmonitor.plugins.viewer.messenger
{
    public sealed class ViewerClientMessenger : IMessenger
    {
        private readonly ViewerReport viewerReport;

        public ViewerClientMessenger(ViewerReport viewerReport)
        {
            this.viewerReport = viewerReport;
        }

        [MessengerId((ushort)ViewerMessengerIds.Server)]
        public void Server(IConnection connection)
        {
            ViewerRunningConfigInfo viewerConfigInfo = MemoryPackSerializer.Deserialize<ViewerRunningConfigInfo>(connection.ReceiveRequestWrap.Payload.Span);
            viewerReport.Server(viewerConfigInfo);
        }

        [MessengerId((ushort)ViewerMessengerIds.Heart)]
        public void Heart(IConnection connection)
        {
            ViewerRunningConfigInfo viewerConfigInfo = MemoryPackSerializer.Deserialize<ViewerRunningConfigInfo>(connection.ReceiveRequestWrap.Payload.Span);
            viewerReport.Heart(viewerConfigInfo);
        }

    }


    public sealed class ViewerServerMessenger : IMessenger
    {
        private readonly MessengerSender messengerSender;
        private readonly SignCaching signCaching;

        public ViewerServerMessenger(MessengerSender messengerSender, SignCaching signCaching)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
        }



        [MessengerId((ushort)ViewerMessengerIds.HeartForward)]
        public void HeartForward(IConnection connection)
        {
            ViewerRunningConfigInfo viewerConfigInfo = MemoryPackSerializer.Deserialize<ViewerRunningConfigInfo>(connection.ReceiveRequestWrap.Payload.Span);
            string[] usernames = viewerConfigInfo.ClientMachines;
            foreach (var item in usernames)
            {
                if (signCaching.Get(item, out SignCacheInfo cache) && cache.Connected)
                {
                    _ = messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = cache.Connection,
                        MessengerId = (ushort)ViewerMessengerIds.Heart,
                        Payload = connection.ReceiveRequestWrap.Payload
                    });
                }
            }
        }
    }
}
