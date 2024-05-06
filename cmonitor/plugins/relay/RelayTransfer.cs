using cmonitor.client;
using cmonitor.config;
using cmonitor.plugins.relay.transport;
using cmonitor.server;
using common.libs;
using common.libs.extends;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace cmonitor.plugins.relay
{
    public sealed class RelayTransfer
    {
        private List<ITransport> transports;

        private readonly Config config;
        private readonly ServiceProvider serviceProvider;

        public Action<RelayTransportState> OnConnected { get; set; } = (state) => { };

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

        public async Task<RelayTransportState> ConnectAsync(string remoteMachineName, string transactionId, string secretKey)
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
                    Socket socket = await transport.RelayAsync(relayInfo);
                    if (socket != null)
                    {
                        return new RelayTransportState { Info = relayInfo, Socket = socket, Direction = RelayTransportDirection.Forward };
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
                Socket socket = await _transports.OnBeginAsync(relayInfo);
                if (socket != null)
                {
                    OnConnected(new RelayTransportState { Info = relayInfo, Socket = socket, Direction = RelayTransportDirection.Reverse });
                    return true;
                }
            }
            return false;
        }

    }
}
