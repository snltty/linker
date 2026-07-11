using linker.tunnel.transport;
using linker.libs;
using System.Net;
using linker.tunnel;
using System.Security.Cryptography.X509Certificates;
using linker.messenger.signin;
using linker.messenger.decenter;
using linker.libs.extends;

namespace linker.messenger.tunnel.client
{
  
    /// <summary>
    /// 打洞信标适配
    /// </summary>
    public class TunnelMessengerAdapter : ITunnelMessengerAdapter
    {
        public string MachineId => signInClientState.Connection?.Id ?? string.Empty;
        public int RouteLevelPlus => tunnelClientStore.RouteLevelPlus;
        public IPEndPoint ServerHost => signInClientState.Connection?.Address ?? null;
        public X509Certificate Certificate => messengerStore.Certificate;
        public X509Certificate CertificateExport => messengerStore.CertificateExport;
        public int PortMapPrivate => tunnelClientStore.PortMapPrivate;
        public int PortMapPublic => tunnelClientStore.PortMapPublic;
        public IPAddress InIp => tunnelClientStore.InIp;


        private readonly IMessengerSender messengerSender;
        private readonly TunnelExclusionPolicyTransfer excludeIPTransfer;
        private readonly ISerializer serializer;
        private readonly ITunnelClientStore tunnelClientStore;
        private readonly SignInClientState signInClientState;
        private readonly IMessengerStore messengerStore;
        private readonly CounterDecenter counterDecenter;

        public TunnelMessengerAdapter(IMessengerSender messengerSender, TunnelExclusionPolicyTransfer excludeIPTransfer,
            ISerializer serializer, ITunnelClientStore tunnelClientStore, SignInClientState signInClientState,
            IMessengerStore messengerStore, CounterDecenter counterDecenter)
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

        public List<TunnelExclusionPolicyInfo> GetExclusionPolicy()
        {
            return excludeIPTransfer.Query();
        }

        public async Task<List<string>> GetTunnelTransportMachineIds()
        {
            return await tunnelClientStore.GetTunnelTransportMachineIds().ConfigureAwait(false);
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
        public async Task<bool> SetTunnelTransports(string machineid, List<ITunnelTransport> list)
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

        public async Task<List<PublicEndpointSample>> LoadRadarSamples()
        {
            return await tunnelClientStore.LoadRadarSamples().ConfigureAwait(false);
        }

        public async Task<bool> SaveRadarSamples(List<PublicEndpointSample> samples)
        {
            LoggerHelper.Instance.Info($"tunnel saving radar samples : {samples.ToJson()}");
            return await tunnelClientStore.SaveRadarSamples(samples).ConfigureAwait(false);
        }
    }
}
