using cmonitor.client.tunnel;
using cmonitor.config;
using cmonitor.plugins.relay.transport;
using common.libs;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Reflection;

namespace cmonitor.plugins.relay
{
    public sealed class RelayTransfer
    {
        private List<ITransport> transports;

        private readonly Config config;
        private readonly ServiceProvider serviceProvider;

        private Dictionary<string, Action<ITunnelConnection>> OnConnected { get; } = new Dictionary<string, Action<ITunnelConnection>>();

        public RelayTransfer(Config config, ServiceProvider serviceProvider)
        {
            this.config = config;
            this.serviceProvider = serviceProvider;
        }

        public void Load(Assembly[] assembs)
        {
            IEnumerable<Type> types = ReflectionHelper.GetInterfaceSchieves(assembs, typeof(ITransport));
            types = config.Data.Common.PluginContains(types);
            transports = types.Select(c => (ITransport)serviceProvider.GetService(c)).Where(c => c != null).Where(c => string.IsNullOrWhiteSpace(c.Name) == false).ToList();

            Logger.Instance.Warning($"load relay transport:{string.Join(",", transports.Select(c => c.Name))}");
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

        public async Task<ITunnelConnection> ConnectAsync(string remoteMachineName, string transactionId, string secretKey)
        {
            IEnumerable<ITransport> _transports = transports.OrderBy(c => c.Name);
            foreach (RelayCompactInfo item in config.Data.Client.Relay.Servers.Where(c => c.Disabled == false))
            {
                ITransport transport = _transports.FirstOrDefault(c => c.Name == item.Name);
                if (transport == null)
                {
                    continue;
                }

                try
                {
                    IPEndPoint server = NetworkHelper.GetEndPoint(item.Host, 3478);
                    RelayInfo relayInfo = new RelayInfo
                    {
                        FlowingId = 0,
                        RemoteMachineName = remoteMachineName,
                        SecretKey = secretKey,
                        Server = server,
                        TransactionId = transactionId,
                        TransportName = transport.Name
                    };
                    ITunnelConnection connection = await transport.RelayAsync(relayInfo);
                    if (connection != null)
                    {
                        return connection;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error(ex);
                }
            }
            return null;
        }
        public async Task<bool> OnBeginAsync(RelayInfo relayInfo)
        {
            ITransport _transports = transports.FirstOrDefault(c => c.Name == relayInfo.TransportName);
            if (_transports != null)
            {
                ITunnelConnection connection = await _transports.OnBeginAsync(relayInfo);
                if (connection != null)
                {
                    if (OnConnected.TryGetValue(connection.TransactionId, out Action<ITunnelConnection> callback))
                    {
                        callback(connection);
                    }
                    return true;
                }
            }
            return false;
        }

    }
}
