using cmonitor.plugins.signIn.messenger;
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
            ViewerConfigInfo viewerConfigInfo = MemoryPackSerializer.Deserialize<ViewerConfigInfo>(connection.ReceiveRequestWrap.Payload.Span);
            viewerReport.Server(viewerConfigInfo);
        }


        [MessengerId((ushort)ViewerMessengerIds.Client)]
        public void Client(IConnection connection)
        {
            ViewerConfigInfo viewerConfigInfo = MemoryPackSerializer.Deserialize<ViewerConfigInfo>(connection.ReceiveRequestWrap.Payload.Span);
            viewerReport.Client(viewerConfigInfo);
        }


        [MessengerId((ushort)ViewerMessengerIds.Heart)]
        public void Heart(IConnection connection)
        {
            string connectStr = MemoryPackSerializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            viewerReport.Heart(connectStr);
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


        [MessengerId((ushort)ViewerMessengerIds.NotifyClient)]
        public void NotifyClient(IConnection connection)
        {
            ViewerConfigInfo viewerConfigInfo = MemoryPackSerializer.Deserialize<ViewerConfigInfo>(connection.ReceiveRequestWrap.Payload.Span);
            string[] usernames = viewerConfigInfo.Clients;
            viewerConfigInfo.Clients = Array.Empty<string>();
            viewerConfigInfo.Mode = ViewerMode.Client;

            byte[] bytes = MemoryPackSerializer.Serialize(viewerConfigInfo);
            foreach (var item in usernames)
            {
                if (signCaching.Get(item, out SignCacheInfo cache) && cache.Connected)
                {
                    _ = messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = cache.Connection,
                        MessengerId = (ushort)ViewerMessengerIds.Client,
                        Payload = bytes
                    });
                }
            }
        }

        [MessengerId((ushort)ViewerMessengerIds.NotifyHeart)]
        public void NotifyHeart(IConnection connection)
        {
            ViewerConfigInfo viewerConfigInfo = MemoryPackSerializer.Deserialize<ViewerConfigInfo>(connection.ReceiveRequestWrap.Payload.Span);
            string[] usernames = viewerConfigInfo.Clients;
            byte[] bytes = MemoryPackSerializer.Serialize(viewerConfigInfo.ConnectStr);
            foreach (var item in usernames)
            {
                if (signCaching.Get(item, out SignCacheInfo cache) && cache.Connected)
                {
                    _ = messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = cache.Connection,
                        MessengerId = (ushort)ViewerMessengerIds.Heart,
                        Payload = bytes
                    });
                }
            }
        }
    }
}
