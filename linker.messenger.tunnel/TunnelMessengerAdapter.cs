using linker.tunnel.transport;
using linker.libs;
using System.Net;
using linker.tunnel;
using linker.messenger;
using linker.messenger.tunnel;
using System.Security.Cryptography.X509Certificates;

namespace linker.plugins.tunnel
{
    /// <summary>
    /// 打洞信标适配存储
    /// </summary>
    public interface ITunnelMessengerAdapterStore
    {
        /// <summary>
        /// 获取信标连接
        /// </summary>
        public IConnection SignConnection { get; }

        /// <summary>
        /// 配置的额外网络层级
        /// </summary>
        public int RouteLevelPlus { get; }

        /// <summary>
        /// 加密密钥
        /// </summary>
        public X509Certificate2 Certificate { get; }

        /// <summary>
        /// 端口映射内外端口
        /// </summary>
        public int PortMapPrivate { get; }
        /// <summary>
        /// 端口映射外网端口
        /// </summary>
        public int PortMapPublic { get; }

        /// <summary>
        /// 获取打洞协议列表
        /// </summary>
        /// <returns></returns>
        public Task<List<TunnelTransportItemInfo>> GetTunnelTransports();
        /// <summary>
        /// 保存打洞协议列表
        /// </summary>
        /// <param name="transports"></param>
        public Task<bool> SetTunnelTransports(List<TunnelTransportItemInfo> list);

    }
    /// <summary>
    /// 打洞信标适配
    /// </summary>
    public class TunnelMessengerAdapter: ITunnelMessengerAdapter
    {
        public string MachineId => tunnelMessengerAdapterStore.SignConnection?.Id ?? string.Empty;
        public int RouteLevelPlus => tunnelMessengerAdapterStore.RouteLevelPlus;
        public IPEndPoint ServerHost => tunnelMessengerAdapterStore.SignConnection?.Address ?? null;
        public X509Certificate2 Certificate => tunnelMessengerAdapterStore.Certificate;
        public int PortMapPrivate => tunnelMessengerAdapterStore.PortMapPrivate;
        public int PortMapPublic => tunnelMessengerAdapterStore.PortMapPublic;


        private readonly IMessengerSender messengerSender;

        private readonly TunnelExcludeIPTransfer excludeIPTransfer;

        private readonly ISerializer serializer;

        private readonly ITunnelMessengerAdapterStore tunnelMessengerAdapterStore;

        public TunnelMessengerAdapter(IMessengerSender messengerSender, TunnelExcludeIPTransfer excludeIPTransfer, ISerializer serializer, ITunnelMessengerAdapterStore tunnelMessengerAdapterStore)
        {
            this.messengerSender = messengerSender;
            this.excludeIPTransfer = excludeIPTransfer;
            this.serializer = serializer;
            this.tunnelMessengerAdapterStore = tunnelMessengerAdapterStore;
        }

        public async Task<List<IPAddress>> GetExcludeIps()
        {
            return await Task.FromResult(excludeIPTransfer.Get());
        }

        public async Task<List<TunnelTransportItemInfo>> GetTunnelTransports()
        {
            return await tunnelMessengerAdapterStore.GetTunnelTransports();
        }

        public async Task<bool> SetTunnelTransports(List<TunnelTransportItemInfo> list)
        {
            return await tunnelMessengerAdapterStore.SetTunnelTransports(list);
        }

        public async Task<TunnelTransportWanPortInfo> GetRemoteWanPort(TunnelWanPortProtocolInfo info)
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = tunnelMessengerAdapterStore.SignConnection,
                MessengerId = (ushort)TunnelMessengerIds.InfoForward,
                Payload = serializer.Serialize(info)
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK && resp.Data.Length > 0)
            {
                return serializer.Deserialize<TunnelTransportWanPortInfo>(resp.Data.Span);
            }
            return null;
        }

        public async Task<bool> SendConnectBegin(TunnelTransportInfo tunnelTransportInfo)
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = tunnelMessengerAdapterStore.SignConnection,
                MessengerId = (ushort)TunnelMessengerIds.BeginForward,
                Payload = serializer.Serialize(tunnelTransportInfo)
            }).ConfigureAwait(false);
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }
        public async Task<bool> SendConnectFail(TunnelTransportInfo tunnelTransportInfo)
        {
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = tunnelMessengerAdapterStore.SignConnection,
                MessengerId = (ushort)TunnelMessengerIds.FailForward,
                Payload = serializer.Serialize(tunnelTransportInfo)
            }).ConfigureAwait(false);
            return true;
        }
        public async Task<bool> SendConnectSuccess(TunnelTransportInfo tunnelTransportInfo)
        {
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = tunnelMessengerAdapterStore.SignConnection,
                MessengerId = (ushort)TunnelMessengerIds.SuccessForward,
                Payload = serializer.Serialize(tunnelTransportInfo)
            }).ConfigureAwait(false);
            return true;
        }

    }
}
