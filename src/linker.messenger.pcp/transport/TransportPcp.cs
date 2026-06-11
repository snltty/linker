using linker.libs;
using linker.libs.extends;
using linker.messenger.signin;
using linker.messenger.tunnel.client;
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

        private readonly PcpNodeTransfer pcpNodeTransfer;

        private readonly ITunnelMessengerAdapter tunnelMessengerAdapter;
        private readonly TunnelTransfer tunnelTransfer;
        private readonly ISignInClientStore signInClientStore;
        private readonly ITunnelClientStore tunnelClientStore;

        public TransportPcp(PcpNodeTransfer pcpNodeTransfer, ITunnelMessengerAdapter tunnelMessengerAdapter, TunnelTransfer tunnelTransfer,
            ISignInClientStore signInClientStore, ITunnelClientStore tunnelClientStore)
        {
            this.pcpNodeTransfer = pcpNodeTransfer;
            this.tunnelMessengerAdapter = tunnelMessengerAdapter;
            this.tunnelTransfer = tunnelTransfer;
            this.signInClientStore = signInClientStore;
            this.tunnelClientStore = tunnelClientStore;

            tunnelTransfer.SetConnectedCallback(_transactionId, _OnConnected);

        }
        private void _OnConnected(ITunnelConnection connection, TunnelTransportInfo info)
        {
            if (connection.Configure.TryGetValue(_transactionId, out string config))
            {
                PcpInfo tag = config.DeJson<PcpInfo>();
                if (tag.NodeId == signInClientStore.Id)
                {
                    if (swapDic.TryRemove(tag.Key, out ITunnelConnection _connection) && _connection.Connected)
                    {
                        connection.TransactionId = tag.TId;
                        connection.Type = TunnelType.PCP;

                        swapTransfer.Swap(_connection, connection, (int)Math.Ceiling(tunnelClientStore.Relay.Bandwidth * 1024 * 1024 / 8.0));
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
                info.TId = tunnelTransportInfo.TransactionId;
                info.Key = $"{tunnelTransportInfo.Local.MachineId}@{tunnelTransportInfo.Remote.MachineId}@{tunnelTransportInfo.TransactionId}";

                var nodes = await pcpNodeTransfer.GetNodeIds(tunnelTransportInfo.Remote.MachineId, info.NodeId).ConfigureAwait(false);
                foreach (var node in nodes)
                {
                    ITunnelConnection connection = null;
                    try
                    {
                        info.NodeId = node;
                        string tag = info.ToJson();
                        tunnelTransportInfo.Configure[_transactionId] = tag;
                        tunnelTransportInfo.Configure["flag"] = _transactionId;

                        connection = await tunnelTransfer.ConnectAsync(node, _transactionId, configures: tunnelTransportInfo.Configure, tunnelTypes: [TunnelType.P2P]).ConfigureAwait(false);
                        if (connection == null)
                        {
                            continue;
                        }
                        connection.RemoteMachineId = tunnelTransportInfo.Remote.MachineId;
                        connection.RemoteMachineName = tunnelTransportInfo.Remote.MachineName;
                        connection.TransactionId = tunnelTransportInfo.TransactionId;
                        connection.TransportName = tunnelTransportInfo.TransportName;
                        connection.Type = TunnelType.PCP;

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
                ITunnelConnection connection = await tunnelTransfer.ConnectAsync(tag.NodeId, _transactionId, configures: tunnelTransportInfo.Configure, tunnelTypes: [TunnelType.P2P]).ConfigureAwait(false);
                if (connection != null)
                {

                    connection.RemoteMachineId = tunnelTransportInfo.Remote.MachineId;
                    connection.RemoteMachineName = tunnelTransportInfo.Remote.MachineName;
                    connection.TransactionId = tunnelTransportInfo.TransactionId;
                    connection.TransportName = tunnelTransportInfo.TransportName;
                    connection.Type = TunnelType.PCP;

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

    }

    public sealed class PcpInfo
    {
        public string NodeId { get; set; }
        public string Key { get; set; }
        public string TId { get; set; }
    }
}
