using cmonitor.libs;
using cmonitor.server;
using MemoryPack;

namespace cmonitor.plugins.share.messenger
{
    public sealed class ShareClientMessenger : IMessenger
    {
        private readonly ShareMemory shareMemory;

        public ShareClientMessenger(ShareMemory shareMemory)
        {
            this.shareMemory = shareMemory;
        }

        [MessengerId((ushort)ShareMessengerIds.Update)]
        public void Update(IConnection connection)
        {
            ShareItemInfo shareItemInfo = MemoryPackSerializer.Deserialize<ShareItemInfo>(connection.ReceiveRequestWrap.Payload.Span);
            shareMemory.Update(shareItemInfo.Index, shareItemInfo.Key, shareItemInfo.Value);
        }
    }

    [MemoryPackable]
    public sealed partial class ShareItemInfo
    {
        /// <summary>
        /// 内存下标
        /// </summary>
        public int Index { get; set; }
        /// <summary>
        /// 内存key，为空则不更新key
        /// </summary>
        public string Key { get; set; } = string.Empty;
        /// <summary>
        /// 内存值
        /// </summary>
        public string Value { get; set; }
    }

}
