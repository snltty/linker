using cmonitor.client;
using cmonitor.client.tunnel;
using cmonitor.config;
using cmonitor.plugins.tunnel.compact;
using cmonitor.plugins.tunnel.messenger;
using cmonitor.plugins.tunnel.transport;
using cmonitor.server;
using common.libs;
using common.libs.extends;
using MemoryPack;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace cmonitor.plugins.tunnel
{
    public sealed class TunnelTransfer
    {
        private List<ITransport> transports;

        private readonly Config config;
        private readonly ServiceProvider serviceProvider;
        private readonly ClientSignInState clientSignInState;
        private readonly MessengerSender messengerSender;
        private readonly CompactTransfer compactTransfer;

        private Dictionary<string, Action<ITunnelConnection>> OnConnected { get; } = new Dictionary<string, Action<ITunnelConnection>>();

        public TunnelTransfer(Config config, ServiceProvider serviceProvider, ClientSignInState clientSignInState, MessengerSender messengerSender, CompactTransfer compactTransfer)
        {
            this.config = config;
            this.serviceProvider = serviceProvider;
            this.clientSignInState = clientSignInState;
            this.messengerSender = messengerSender;
            this.compactTransfer = compactTransfer;
        }

        public void Load(Assembly[] assembs)
        {
            IEnumerable<Type> types = ReflectionHelper.GetInterfaceSchieves(assembs, typeof(ITransport));
            types = config.Data.Common.PluginContains(types);
            transports = types.Select(c => (ITransport)serviceProvider.GetService(c)).Where(c => c != null).Where(c => string.IsNullOrWhiteSpace(c.Name) == false).ToList();
            foreach (var item in transports)
            {
                item.OnSendConnectBegin = OnSendConnectBegin;
                item.OnSendConnectFail = OnSendConnectFail;
                item.OnConnectBegin = OnConnectBegin;
                item.OnConnecting = OnConnecting;
                item.OnConnected = _OnConnected;
                item.OnConnectFail = OnConnectFail;
            }

            Logger.Instance.Warning($"load tunnel transport:{string.Join(",", transports.Select(c => c.Name))}");
        }

        public async Task<ITunnelConnection> ConnectAsync(string remoteMachineName, string transactionId)
        {
            IEnumerable<ITransport> _transports = transports.OrderBy(c => c.ProtocolType);
            foreach (ITransport transport in _transports)
            {
                //获取自己的外网ip
                TunnelTransportExternalIPInfo localInfo = await GetLocalInfo(transport.ProtocolType);
                if (localInfo == null)
                {
                    continue;
                }
                //获取对方的外网ip
                TunnelTransportExternalIPInfo remoteInfo = await GetRemoteInfo(remoteMachineName, transport.ProtocolType);
                if (remoteInfo == null)
                {
                    continue;
                }
                TunnelTransportInfo tunnelTransportInfo = new TunnelTransportInfo
                {
                    Direction = TunnelDirection.Forward,
                    TransactionId = transactionId,
                    TransportName = transport.Name,
                    TransportType = transport.ProtocolType,
                    Local = localInfo,
                    Remote = remoteInfo,
                };
                ITunnelConnection connection = await transport.ConnectAsync(tunnelTransportInfo);
                if (connection != null)
                {
                    _OnConnected(connection);
                    return connection;
                }
            }
            return null;
        }
        public void OnBegin(TunnelTransportInfo tunnelTransportInfo)
        {
            ITransport _transports = transports.FirstOrDefault(c => c.Name == tunnelTransportInfo.TransportName && c.ProtocolType == tunnelTransportInfo.TransportType);
            if (_transports != null)
            {
                _transports.OnBegin(tunnelTransportInfo);
            }
        }
        public void OnFail(TunnelTransportInfo tunnelTransportInfo)
        {
            ITransport _transports = transports.FirstOrDefault(c => c.Name == tunnelTransportInfo.TransportName && c.ProtocolType == tunnelTransportInfo.TransportType);
            if (_transports != null)
            {
                _transports.OnFail(tunnelTransportInfo);
                OnConnectFail(tunnelTransportInfo.Remote.MachineName);
            }
        }
        public async Task<TunnelTransportExternalIPInfo> Info(TunnelTransportExternalIPRequestInfo request)
        {
            return await GetLocalInfo(request.TransportType);
        }

        private async Task<TunnelTransportExternalIPInfo> GetLocalInfo(TunnelProtocolType transportType)
        {
            TunnelCompactIPEndPoint[] ips = await compactTransfer.GetExternalIPAsync(transportType);
            if (ips != null && ips.Length > 0)
            {
                return new TunnelTransportExternalIPInfo
                {
                    Local = ips[0].Local,
                    Remote = ips[0].Remote,
                    LocalIps = config.Data.Client.Tunnel.LocalIPs,
                    RouteLevel = config.Data.Client.Tunnel.RouteLevel,
                    MachineName = config.Data.Client.Name
                };
            }
            return null;
        }
        private async Task<TunnelTransportExternalIPInfo> GetRemoteInfo(string remoteMachineName, TunnelProtocolType transportType)
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)TunnelMessengerIds.InfoForward,
                Timeout = 3000,
                Payload = MemoryPackSerializer.Serialize(new TunnelTransportExternalIPRequestInfo
                {
                    RemoteMachineName = remoteMachineName,
                    TransportType = transportType,
                })
            });
            if (resp.Code == MessageResponeCodes.OK && resp.Data.Length > 0)
            {
                return MemoryPackSerializer.Deserialize<TunnelTransportExternalIPInfo>(resp.Data.Span);
            }
            return null;

        }

        private async Task<bool> OnSendConnectBegin(TunnelTransportInfo tunnelTransportInfo)
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)TunnelMessengerIds.BeginForward,
                Payload = MemoryPackSerializer.Serialize(tunnelTransportInfo)
            });
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }
        private async Task OnSendConnectFail(TunnelTransportInfo tunnelTransportInfo)
        {
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)TunnelMessengerIds.FailForward,
                Payload = MemoryPackSerializer.Serialize(tunnelTransportInfo)
            });
        }


        public void SetConnectCallback(string transactionId, Action<ITunnelConnection> callback)
        {
            if (OnConnected.TryGetValue(transactionId, out Action<ITunnelConnection> _callback) == false)
            {
                OnConnected[transactionId] = callback;
            }
            else
            {
                OnConnected[transactionId] += callback;
            }
        }


        private void OnConnecting(TunnelTransportInfo tunnelTransportInfo)
        {
            if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                Logger.Instance.Debug($"tunnel connecting {tunnelTransportInfo.Remote.MachineName},{tunnelTransportInfo.ToJson()}");
        }
        private void OnConnectBegin(TunnelTransportInfo tunnelTransportInfo)
        {
            if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                Logger.Instance.Debug($"tunnel connecting from {tunnelTransportInfo.Remote.MachineName},{tunnelTransportInfo.ToJson()}");
        }
        private void _OnConnected(ITunnelConnection connection)
        {
            if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                Logger.Instance.Debug($"tunnel connect {connection.RemoteMachineName} success");
            if (OnConnected.TryGetValue(connection.TransactionId, out Action<ITunnelConnection> _callback))
            {
                _callback(connection);
            }
        }
        public void OnDisConnected(ITunnelConnection connection)
        {

        }
        private void OnConnectFail(string machineName)
        {
            Logger.Instance.Error($"tunnel connect {machineName} fail");
        }
    }
}
