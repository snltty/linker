using cmonitor.libs;
using MemoryPack;

namespace cmonitor.service.messengers.share
{
    public sealed class ShareMessenger : IMessenger
    {
        private readonly ShareMemory shareMemory;

        public ShareMessenger(ShareMemory shareMemory)
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
