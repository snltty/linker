using linker.tunnel.transport;
using linker.libs;
using System.Net;
using linker.tunnel;
using System.Security.Cryptography.X509Certificates;
using linker.messenger.signin;
using linker.messenger.decenter;

namespace linker.messenger.tunnel
{
    /// <summary>
    /// 打洞信标适配存储
    /// </summary>
    public interface ITunnelClientStore
    {
        public int TransportMachineIdCount { get; }
        /// <summary>
        /// 配置的额外网络层级
        /// </summary>
        public int RouteLevelPlus { get; }
        /// <summary>
        /// 设置额外的网关层级
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public Task<bool> SetRouteLevelPlus(int level);

        /// <summary>
        /// 端口映射内外端口
        /// </summary>
        public int PortMapPrivate { get; }
        /// <summary>
        /// 端口映射外网端口
        /// </summary>
        public int PortMapPublic { get; }
        /// <summary>
        /// 设置映射端口
        /// </summary>
        /// <param name="privatePort"></param>
        /// <param name="publicPort"></param>
        /// <returns></returns>
        public Task<bool> SetPortMap(int privatePort, int publicPort);

        /// <summary>
        /// 获取打洞协议列表
        /// </summary>
        /// <returns></returns>
        public Task<List<TunnelTransportItemInfo>> GetTunnelTransports(string machineId);
        /// <summary>
        /// 保存打洞协议列表
        /// </summary>
        /// <param name="list"></param>
        public Task<bool> SetTunnelTransports(string machineId, List<TunnelTransportItemInfo> list);


        public Action OnChanged { get; set; }



        /// <summary>
        /// 配置的额外网络层级
        /// </summary>
        public TunnelPublicNetworkInfo Network { get; }
        /// <summary>
        /// 设置额外的网关层级
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public Task<bool> SetNetwork(TunnelPublicNetworkInfo network);
    }
    /// <summary>
    /// 打洞信标适配
    /// </summary>
    public class TunnelMessengerAdapter : ITunnelMessengerAdapter
    {
        public string MachineId => signInClientState.Connection?.Id ?? string.Empty;
        public int RouteLevelPlus => tunnelClientStore.RouteLevelPlus;
        public IPEndPoint ServerHost => signInClientState.Connection?.Address ?? null;
        public X509Certificate Certificate => messengerStore.Certificate;
        public int PortMapPrivate => tunnelClientStore.PortMapPrivate;
        public int PortMapPublic => tunnelClientStore.PortMapPublic;


        private readonly IMessengerSender messengerSender;
        private readonly TunnelClientExcludeIPTransfer excludeIPTransfer;
        private readonly ISerializer serializer;
        private readonly ITunnelClientStore tunnelClientStore;
        private readonly SignInClientState signInClientState;
        private readonly IMessengerStore messengerStore;
        private readonly CounterDecenter counterDecenter;

        public TunnelMessengerAdapter(IMessengerSender messengerSender, TunnelClientExcludeIPTransfer excludeIPTransfer,
            ISerializer serializer, ITunnelClientStore tunnelClientStore, SignInClientState signInClientState, IMessengerStore messengerStore, CounterDecenter counterDecenter)
        {
            this.messengerSender = messengerSender;
            this.excludeIPTransfer = excludeIPTransfer;
            this.serializer = serializer;
            this.tunnelClientStore = tunnelClientStore;
            this.signInClientState = signInClientState;
            this.messengerStore = messengerStore;
            this.counterDecenter = counterDecenter;

            SetCounter();
        }

        public List<TunnelExIPInfo> GetExcludeIps()
        {
            return excludeIPTransfer.Get();
        }

        public async Task<List<TunnelTransportItemInfo>> GetTunnelTransports(string machineid)
        {
            return await tunnelClientStore.GetTunnelTransports(machineid).ConfigureAwait(false);
        }

        public async Task<bool> SetTunnelTransports(string machineid, List<TunnelTransportItemInfo> list)
        {
            bool res = await tunnelClientStore.SetTunnelTransports(machineid, list).ConfigureAwait(false);
            SetCounter();
            return res;
        }
        private void SetCounter()
        {
            counterDecenter.SetValue($"transport", tunnelClientStore.TransportMachineIdCount);
        }


        public async Task<TunnelTransportWanPortInfo> GetRemoteWanPort(TunnelWanPortProtocolInfo info)
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
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
                Connection = signInClientState.Connection,
                MessengerId = (ushort)TunnelMessengerIds.BeginForward,
                Payload = serializer.Serialize(tunnelTransportInfo)
            }).ConfigureAwait(false);
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }
        public async Task<bool> SendConnectFail(TunnelTransportInfo tunnelTransportInfo)
        {
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)TunnelMessengerIds.FailForward,
                Payload = serializer.Serialize(tunnelTransportInfo)
            }).ConfigureAwait(false);
            return true;
        }
        public async Task<bool> SendConnectSuccess(TunnelTransportInfo tunnelTransportInfo)
        {
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)TunnelMessengerIds.SuccessForward,
                Payload = serializer.Serialize(tunnelTransportInfo)
            }).ConfigureAwait(false);
            return true;
        }

    }
}
