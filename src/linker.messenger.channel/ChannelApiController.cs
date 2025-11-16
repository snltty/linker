using linker.libs.extends;
using System.Collections.Concurrent;
using linker.tunnel.connection;
using linker.messenger.api;
using linker.libs.web;

namespace linker.messenger.channel
{
    public sealed class ChannelApiController : IApiController
    {
        private readonly ChannelConnectionCaching channelConnectionCaching;
        public ChannelApiController(ChannelConnectionCaching channelConnectionCaching)
        {
            this.channelConnectionCaching = channelConnectionCaching;
        }

        public ConnectionListInfo Get(ApiControllerParamsInfo param)
        {
            ulong hashCode = ulong.Parse(param.Content);
            if (channelConnectionCaching.Version.Eq(hashCode, out ulong version) == false)
            {
                return new ConnectionListInfo
                {
                    List = channelConnectionCaching.Connections,
                    HashCode = version
                };
            }
            return new ConnectionListInfo { HashCode = version };
        }

        [Access(AccessValue.TunnelRemove)]
        public void Remove(ApiControllerParamsInfo param)
        {
            RemoveInfo info = param.Content.DeJson<RemoveInfo>();
            channelConnectionCaching.Remove(info.MachineId, info.TransactionId);
        }


    }

    public sealed class RemoveInfo
    {
        public string MachineId { get; set; }
        public string TransactionId { get; set; }
    }

    public sealed class ConnectionListInfo
    {
        public ConcurrentDictionary<string, ConcurrentDictionary<string, ITunnelConnection>> List { get; set; }
        public ulong HashCode { get; set; }
    }

}
