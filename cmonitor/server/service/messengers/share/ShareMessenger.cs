using cmonitor.server.client.reports.share;
using MemoryPack;

namespace cmonitor.server.service.messengers.share
{
    public sealed class ShareMessenger : IMessenger
    {
        private readonly ShareReport shareReport;

        public ShareMessenger(ShareReport shareReport)
        {
            this.shareReport = shareReport;
        }

        [MessengerId((ushort)ShareMessengerIds.Update)]
        public void Update(IConnection connection)
        {
            ShareItemInfo shareItemInfo = MemoryPackSerializer.Deserialize<ShareItemInfo>(connection.ReceiveRequestWrap.Payload.Span);
            shareReport.UpdateShare(shareItemInfo);
        }
    }

}
