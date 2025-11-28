using linker.libs;
using linker.libs.timer;
using linker.messenger.relay.messenger;

namespace linker.messenger.relay.server
{
    public sealed class RelayServerNodeReportTransfer
    {
        private int connectionNum = 0;
        private ulong bytes = 0;
        private ulong lastBytes = 0;

        public int ConnectionNum => connectionNum;

        private readonly RelayServerConnectionTransfer relayServerConnectionTransfer;
        private readonly IRelayServerConfigStore relayServerConfigStore;
        private readonly ISerializer serializer;
        private readonly IMessengerSender messengerSender;
        private readonly IRelayServerNodeStore relayServerNodeStore;

        public RelayServerNodeReportTransfer(RelayServerConnectionTransfer relayServerConnectionTransfer, IRelayServerConfigStore relayServerConfigStore,
            ISerializer serializer, IMessengerSender messengerSender, IRelayServerNodeStore relayServerNodeStore)
        {
            this.relayServerConnectionTransfer = relayServerConnectionTransfer;
            this.relayServerConfigStore = relayServerConfigStore;
            this.serializer = serializer;
            this.messengerSender = messengerSender;
            this.relayServerNodeStore = relayServerNodeStore;

            ReportTask();

        }

        public void IncrementConnectionNum()
        {
            Interlocked.Increment(ref connectionNum);
        }

        public void DecrementConnectionNum()
        {
            Interlocked.Decrement(ref connectionNum);
        }

        public void AddBytes(long length)
        {
            Interlocked.Add(ref bytes, (ulong)length);
        }

        public async Task<bool> Report(RelayServerNodeReportInfo info)
        {
            return await relayServerNodeStore.Report(info).ConfigureAwait(false);
        }

        private void ReportTask()
        {
            TimerHelper.SetIntervalLong(async () =>
            {
                try
                {
                    double diff = (bytes - lastBytes) * 8 / 1024.0 / 1024.0;
                    lastBytes = bytes;

                    var config = relayServerConfigStore.Config;
                    RelayServerNodeReportInfo info = new RelayServerNodeReportInfo
                    {
                        Bandwidth = config.Bandwidth,
                        Connections = config.Connections,
                        DataEachMonth = config.DataEachMonth,
                        DataRemain = config.DataRemain,
                        Host = config.Host,
                        Logo = config.Logo,
                        Name = config.Name,
                        NodeId = config.NodeId,
                        Protocol = config.Protocol,
                        Url = config.Url,
                        ConnectionsRatio = connectionNum,
                        BandwidthRatio = Math.Round(diff / 5, 2),
                        Version = VersionHelper.Version,
                    };
                    byte[] memory = serializer.Serialize(info);
                    var tasks = relayServerConnectionTransfer.Get().Select(c => messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = c,
                        MessengerId = (ushort)RelayMessengerIds.Report,
                        Payload = memory,
                        Timeout = 5000
                    })).ToList();
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        LoggerHelper.Instance.Error($"relay report : {ex}");
                    }
                }
            }, 5000);
        }
    }
}
