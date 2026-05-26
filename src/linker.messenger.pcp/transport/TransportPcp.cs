using linker.libs;
using linker.libs.extends;
using linker.messenger.signin;
using linker.tunnel;
using linker.tunnel.connection;
using linker.tunnel.transport;
using linker.tunnel.wanport;
using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;

namespace linker.messenger.pcp
{
    public class TransportPcp : ITunnelTransport
    {
        public string Name => "PCP";

        public string Label => "PCP、节点中继";

        public TunnelProtocolType ProtocolType => TunnelProtocolType.All;

        public TunnelWanPortProtocolType AllowWanPortProtocolType => TunnelWanPortProtocolType.Other;
        public TunnelType TunnelType => TunnelType.PCP;

        public bool Reverse => false;

        public bool DisableReverse => true;

        public bool SSL => true;

        public bool DisableSSL => true;

        public byte Order => 255;

        public Action<ITunnelConnection, TunnelTransportInfo> OnConnected { get; set; } = (state, info) => { };


        private readonly string _transactionId = "pcp";
        private readonly ConcurrentDictionary<string, TaskCompletionSource<bool>> watingDic = new();
        private readonly ConcurrentDictionary<string, ITunnelConnection> swapDic = new();
        private readonly SwapTransfer swapTransfer = new SwapTransfer();

        private readonly IPcpStore pcpStore;

        private readonly IMessengerSender messengerSender;
        private readonly ISerializer serializer;
        private readonly SignInClientState signInClientState;
        private readonly ITunnelMessengerAdapter tunnelMessengerAdapter;
        private readonly TunnelTransfer tunnelTransfer;
        private readonly SignInClientTransfer signInClientTransfer;
        private readonly ISignInClientStore signInClientStore;

        public TransportPcp(IPcpStore pcpStore, IMessengerSender messengerSender, ISerializer serializer,
            SignInClientState signInClientState, ITunnelMessengerAdapter tunnelMessengerAdapter,
            TunnelTransfer tunnelTransfer, SignInClientTransfer signInClientTransfer, ISignInClientStore signInClientStore)
        {
            this.pcpStore = pcpStore;
            this.messengerSender = messengerSender;
            this.serializer = serializer;
            this.signInClientState = signInClientState;
            this.tunnelMessengerAdapter = tunnelMessengerAdapter;
            this.tunnelTransfer = tunnelTransfer;
            this.signInClientTransfer = signInClientTransfer;
            this.signInClientStore = signInClientStore;

            tunnelTransfer.SetConnectedCallback(_transactionId, _OnConnected);
        }
        private void _OnConnected(ITunnelConnection connection)
        {
            if (connection.Configure.TryGetValue(_transactionId, out string config))
            {
                PcpInfo tag = config.DeJson<PcpInfo>();
                if (tag.NodeId == signInClientStore.Id)
                {
                    if (swapDic.TryRemove(tag.Key, out ITunnelConnection _connection) && _connection.Connected)
                    {
                        swapTransfer.Swap(_connection, connection);
                    }
                    else
                    {
                        swapDic.AddOrUpdate(tag.Key, connection, (k, v) => connection);
                    }
                }
            }
        }

        public void SetSSL(X509Certificate certificate)
        {
        }

        public virtual async Task<ITunnelConnection> ConnectAsync(TunnelTransportInfo tunnelTransportInfo)
        {
            try
            {
                PcpInfo info = tunnelTransportInfo.Configure[_transactionId].DeJson<PcpInfo>();
                info.Key = $"{tunnelTransportInfo.Local.MachineId}@{tunnelTransportInfo.Remote.MachineId}@{tunnelTransportInfo.TransactionId}";

                List<string> offlines = await signInClientTransfer.GetOfflines(pcpStore.PcpHistory.History).ConfigureAwait(false);
                List<string> remoteNodes = await GetNodes(tunnelTransportInfo.Remote.MachineId).ConfigureAwait(false);
                List<string> nodes = pcpStore.PcpHistory.History.Intersect(remoteNodes).Except(offlines).ToList();

                foreach (var node in nodes.Where(c => c == info.NodeId).Concat(nodes.Where(c => c != info.NodeId)))
                {
                    ITunnelConnection connection = null;
                    try
                    {
                        info.NodeId = node;
                        string tag = info.ToJson();
                        tunnelTransportInfo.Configure[_transactionId] = tag;

                        Dictionary<string, string> configures = new() { ["flag"] = _transactionId, [_transactionId] = tag };

                        connection = await tunnelTransfer.ConnectAsync(node, _transactionId, TunnelProtocolType.None, tunnelTypes: [TunnelType.P2P], configures: configures).ConfigureAwait(false);
                        if (connection == null)
                        {
                            continue;
                        }
                        if (await tunnelMessengerAdapter.SendConnectBegin(tunnelTransportInfo).ConfigureAwait(false) == false)
                        {
                            throw new Exception("pcp client begin fail");
                        }

                        TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
                        watingDic.AddOrUpdate(info.Key, tcs, (k, v) => tcs);
                        bool result = await tcs.Task.WaitAsync(TimeSpan.FromMilliseconds(600000)).ConfigureAwait(false);
                        if (result)
                        {
                            await tunnelMessengerAdapter.SendConnectSuccess(tunnelTransportInfo).ConfigureAwait(false);
                            return connection;
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggerHelper.Instance.Error(ex);
                        connection?.Dispose();
                    }
                    finally
                    {
                        watingDic.TryRemove(info.Key, out _);
                    }
                }
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
            }
            await tunnelMessengerAdapter.SendConnectFail(tunnelTransportInfo).ConfigureAwait(false);
            return null;
        }
        public virtual async Task OnBegin(TunnelTransportInfo tunnelTransportInfo)
        {
            PcpInfo tag = tunnelTransportInfo.Configure[_transactionId].DeJson<PcpInfo>();
            try
            {
                Dictionary<string, string> configures = new() { ["flag"] = _transactionId, [_transactionId] = tunnelTransportInfo.Configure[_transactionId] };
                ITunnelConnection connection = await tunnelTransfer.ConnectAsync(tag.NodeId, _transactionId, TunnelProtocolType.None, tunnelTypes: [TunnelType.P2P], configures: configures).ConfigureAwait(false);
                if (connection != null)
                {
                    OnConnected(connection, tunnelTransportInfo);
                    await tunnelMessengerAdapter.SendConnectSuccess(tunnelTransportInfo).ConfigureAwait(false);
                    return;
                }
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
            }
            OnConnected(null, tunnelTransportInfo);
            await tunnelMessengerAdapter.SendConnectFail(tunnelTransportInfo).ConfigureAwait(false);
        }
        public virtual void OnFail(TunnelTransportInfo tunnelTransportInfo)
        {
            PcpInfo info = tunnelTransportInfo.Configure[_transactionId].DeJson<PcpInfo>();
            if (watingDic.TryRemove(info.Key, out TaskCompletionSource<bool> tcs))
            {
                tcs.SetResult(false);
            }
        }
        public virtual void OnSuccess(TunnelTransportInfo tunnelTransportInfo)
        {
            PcpInfo info = tunnelTransportInfo.Configure[_transactionId].DeJson<PcpInfo>();
            if (watingDic.TryRemove(info.Key, out TaskCompletionSource<bool> tcs))
            {
                tcs.SetResult(true);
            }
        }

        private async Task<List<string>> GetNodes(string machineId)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)PcpMessengerIds.NodesForward,
                Payload = serializer.Serialize(machineId),
                Timeout = 5000,
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK)
            {
                return serializer.Deserialize<List<string>>(resp.Data.Span);
            }
            return [];
        }
    }

    public sealed class PcpInfo
    {
        public string NodeId { get; set; }
        public string Key { get; set; }
    }
}
