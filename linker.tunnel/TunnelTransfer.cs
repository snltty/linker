using Linker.Tunnel.Adapter;
using Linker.Tunnel.Connection;
using Linker.Tunnel.Transport;
using Linker.Libs;
using Linker.Libs.Extends;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net;
using Linker.Tunnel.WanPort;

namespace Linker.Tunnel
{
    public sealed class TunnelTransfer
    {
        private List<ITunnelTransport> transports;
        private TunnelWanPortTransfer compactTransfer;
        private ITunnelAdapter  tunnelAdapter ;

        private ConcurrentDictionary<string, bool> connectingDic = new ConcurrentDictionary<string, bool>();
        private Dictionary<string, List<Action<ITunnelConnection>>> OnConnected { get; } = new Dictionary<string, List<Action<ITunnelConnection>>>();

        public TunnelTransfer()
        {
        }

        /// <summary>
        /// 加载打洞协议
        /// </summary>
        /// <param name="assembs"></param>
        public void Init(TunnelWanPortTransfer compactTransfer, ITunnelAdapter tunnelAdapter, List<ITunnelTransport> transports)
        {
            this.compactTransfer = compactTransfer;
            this.tunnelAdapter = tunnelAdapter;
            this.transports = transports;

            foreach (var item in transports)
            {
                item.OnSendConnectBegin = tunnelAdapter.SendConnectBegin;
                item.OnSendConnectFail = tunnelAdapter.SendConnectFail;
                item.OnSendConnectSuccess = tunnelAdapter.SendConnectSuccess;
                item.OnConnected = _OnConnected;
            }

            var transportItems = tunnelAdapter.GetTunnelTransports();
            transportItems = transportItems.Concat(transports.Select(c => new TunnelTransportItemInfo { Label = c.Label, Name = c.Name, ProtocolType = c.ProtocolType.ToString() }))
                .Distinct(new TunnelTransportItemInfoEqualityComparer())
                .Where(c=> transports.Select(c=>c.Name).Contains(c.Name))
                .ToList();


            tunnelAdapter.SetTunnelTransports(transportItems);

            LoggerHelper.Instance.Warning($"load tunnel transport:{string.Join(",", transports.Select(c => c.Name))}");
            LoggerHelper.Instance.Warning($"used tunnel transport:{string.Join(",", transportItems.Where(c => c.Disabled == false).Select(c => c.Name))}");
        }


        /// <summary>
        /// 设置成功打洞回调
        /// </summary>
        /// <param name="transactionId"></param>
        /// <param name="callback"></param>
        public void SetConnectedCallback(string transactionId, Action<ITunnelConnection> callback)
        {
            if (OnConnected.TryGetValue(transactionId, out List<Action<ITunnelConnection>> callbacks) == false)
            {
                callbacks = new List<Action<ITunnelConnection>>();
                OnConnected[transactionId] = callbacks;
            }
            callbacks.Add(callback);
        }
        /// <summary>
        /// 移除打洞成功回调
        /// </summary>
        /// <param name="transactionId"></param>
        /// <param name="callback"></param>
        public void RemoveConnectedCallback(string transactionId, Action<ITunnelConnection> callback)
        {
            if (OnConnected.TryGetValue(transactionId, out List<Action<ITunnelConnection>> callbacks))
            {
                callbacks.Remove(callback);
            }
        }


        /// <summary>
        /// 开始连接对方
        /// </summary>
        /// <param name="remoteMachineId">对方id</param>
        /// <param name="transactionId">事务id，随便起，你喜欢就好</param>
        /// <returns></returns>
        public async Task<ITunnelConnection> ConnectAsync(string remoteMachineId, string transactionId)
        {
            if (connectingDic.TryAdd(remoteMachineId, true) == false)
            {
                return null;
            }

            try
            {
                foreach (TunnelTransportItemInfo transportItem in tunnelAdapter.GetTunnelTransports().Where(c => c.Disabled == false))
                {
                    ITunnelTransport transport = transports.FirstOrDefault(c => c.Name == transportItem.Name);
                    if (transport == null) continue;

                    TunnelTransportInfo tunnelTransportInfo = null;
                    //是否在失败后尝试反向连接
                    int times = transportItem.Reverse ? 1 : 0;
                    for (int i = 0; i <= times; i++)
                    {
                        try
                        {
                            //获取自己的外网ip
                            TunnelTransportWanPortInfo localInfo = await GetLocalInfo();
                            if (localInfo == null)
                            {
                                LoggerHelper.Instance.Error($"tunnel {transport.Name} get local external ip fail ");
                                break;
                            }
                            LoggerHelper.Instance.Info($"tunnel {transport.Name} got local external ip {localInfo.ToJson()}");
                            //获取对方的外网ip
                            TunnelTransportWanPortInfo remoteInfo = await tunnelAdapter.GetRemoteWanPort(remoteMachineId);
                            if (remoteInfo == null)
                            {
                                LoggerHelper.Instance.Error($"tunnel {transport.Name} get remote {remoteMachineId} external ip fail ");
                                break;
                            }
                            LoggerHelper.Instance.Info($"tunnel {transport.Name} got remote external ip {remoteInfo.ToJson()}");

                            tunnelTransportInfo = new TunnelTransportInfo
                            {
                                Direction = (TunnelDirection)i,
                                TransactionId = transactionId,
                                TransportName = transport.Name,
                                TransportType = transport.ProtocolType,
                                Local = localInfo,
                                Remote = remoteInfo,
                                SSL = transportItem.SSL
                            };
                            OnConnecting(tunnelTransportInfo);
                            ParseRemoteEndPoint(tunnelTransportInfo);
                            ITunnelConnection connection = await transport.ConnectAsync(tunnelTransportInfo);
                            if (connection != null)
                            {
                                _OnConnected(connection);
                                return connection;
                            }
                        }
                        catch (Exception ex)
                        {
                            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                            {
                                LoggerHelper.Instance.Error(ex);
                            }
                        }
                    }
                    if (tunnelTransportInfo != null)
                    {
                        OnConnectFail(tunnelTransportInfo);
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
            finally
            {
                connectingDic.TryRemove(remoteMachineId, out _);
            }

            return null;
        }
        /// <summary>
        /// 收到对方开始连接的消息
        /// </summary>
        /// <param name="tunnelTransportInfo"></param>
        public void OnBegin(TunnelTransportInfo tunnelTransportInfo)
        {
            if (connectingDic.TryAdd(tunnelTransportInfo.Remote.MachineId, true) == false)
            {
                return;
            }
            try
            {
                ITunnelTransport _transports = transports.FirstOrDefault(c => c.Name == tunnelTransportInfo.TransportName && c.ProtocolType == tunnelTransportInfo.TransportType);
                if (_transports != null)
                {
                    OnConnectBegin(tunnelTransportInfo);
                    ParseRemoteEndPoint(tunnelTransportInfo);
                    _transports.OnBegin(tunnelTransportInfo).ContinueWith((result) =>
                    {
                        connectingDic.TryRemove(tunnelTransportInfo.Remote.MachineId, out _);
                    });
                }
                else
                {
                    connectingDic.TryRemove(tunnelTransportInfo.Remote.MachineId, out _);
                }
            }
            catch (Exception ex)
            {
                connectingDic.TryRemove(tunnelTransportInfo.Remote.MachineId, out _);
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
            }
        }
        /// <summary>
        /// 收到对方发来的连接失败的消息
        /// </summary>
        /// <param name="tunnelTransportInfo"></param>
        public void OnFail(TunnelTransportInfo tunnelTransportInfo)
        {
            ITunnelTransport _transports = transports.FirstOrDefault(c => c.Name == tunnelTransportInfo.TransportName && c.ProtocolType == tunnelTransportInfo.TransportType);
            if (_transports != null)
            {
                _transports.OnFail(tunnelTransportInfo);
            }
        }
        /// <summary>
        /// 收到对方发来的连接成功的消息
        /// </summary>
        /// <param name="tunnelTransportInfo"></param>
        public void OnSuccess(TunnelTransportInfo tunnelTransportInfo)
        {
            ITunnelTransport _transports = transports.FirstOrDefault(c => c.Name == tunnelTransportInfo.TransportName && c.ProtocolType == tunnelTransportInfo.TransportType);
            if (_transports != null)
            {
                _transports.OnSuccess(tunnelTransportInfo);
            }
        }

        /// <summary>
        /// 获取自己的外网IP，给别人调用
        /// </summary>
        /// <returns></returns>
        public async Task<TunnelTransportWanPortInfo> GetWanPort()
        {
            return await GetLocalInfo();
        }
        /// <summary>
        /// 获取自己的外网IP
        /// </summary>
        /// <returns></returns>
        private async Task<TunnelTransportWanPortInfo> GetLocalInfo()
        {
            TunnelWanPortEndPoint ip = await compactTransfer.GetWanPortAsync(tunnelAdapter.LocalIP);
            if (ip != null)
            {
                var config = tunnelAdapter.GetLocalConfig();
                return new TunnelTransportWanPortInfo
                {
                    Local = ip.Local,
                    Remote = ip.Remote,
                    LocalIps = config.LocalIps,
                    RouteLevel = config.RouteLevel,
                    MachineId = config.MachineId
                };
            }
            return null;
        }

        private void OnConnecting(TunnelTransportInfo tunnelTransportInfo)
        {
            //if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
            LoggerHelper.Instance.Info($"tunnel connecting {tunnelTransportInfo.Remote.MachineId}->{tunnelTransportInfo.Remote.MachineName},{tunnelTransportInfo.ToJson()}");
        }
        private void OnConnectBegin(TunnelTransportInfo tunnelTransportInfo)
        {
            //if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
            LoggerHelper.Instance.Info($"tunnel connecting from {tunnelTransportInfo.Remote.MachineId}->{tunnelTransportInfo.Remote.MachineName},{tunnelTransportInfo.ToJson()}");
        }

        /// <summary>
        /// 连接成功
        /// </summary>
        /// <param name="connection"></param>
        private void _OnConnected(ITunnelConnection connection)
        {
            //if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
            LoggerHelper.Instance.Debug($"tunnel connect {connection.RemoteMachineId}->{connection.RemoteMachineName} success->{connection.IPEndPoint},{connection.ToJsonFormat()}");

            //调用以下别人注册的回调
            if (OnConnected.TryGetValue(Helper.GlobalString, out List<Action<ITunnelConnection>> callbacks))
            {
                foreach (var item in callbacks)
                {
                    item(connection);
                }
            }
            if (OnConnected.TryGetValue(connection.TransactionId, out callbacks))
            {
                foreach (var item in callbacks)
                {
                    item(connection);
                }
            }
        }
        private void OnConnectFail(TunnelTransportInfo tunnelTransportInfo)
        {
            LoggerHelper.Instance.Error($"tunnel connect {tunnelTransportInfo.Remote.MachineId} fail->{tunnelTransportInfo.ToJsonFormat()}");
        }

        private void ParseRemoteEndPoint(TunnelTransportInfo tunnelTransportInfo)
        {
            //要连接哪些IP
            IPAddress[] localIps = tunnelTransportInfo.Remote.LocalIps.Where(c => c.Equals(tunnelTransportInfo.Remote.Local.Address) == false).ToArray();
            List<IPEndPoint> eps = new List<IPEndPoint>();

            //先尝试内网ipv4
            foreach (IPAddress item in localIps.Where(c => c.AddressFamily == AddressFamily.InterNetwork))
            {
                eps.Add(new IPEndPoint(item, tunnelTransportInfo.Remote.Local.Port));
                eps.Add(new IPEndPoint(item, tunnelTransportInfo.Remote.Remote.Port));
                eps.Add(new IPEndPoint(item, tunnelTransportInfo.Remote.Remote.Port + 1));
            }
            //在尝试外网
            eps.AddRange(new List<IPEndPoint>{
                new IPEndPoint(tunnelTransportInfo.Remote.Remote.Address,tunnelTransportInfo.Remote.Remote.Port),
                new IPEndPoint(tunnelTransportInfo.Remote.Remote.Address,tunnelTransportInfo.Remote.Remote.Port+1),
            });
            //再尝试IPV6
            foreach (IPAddress item in localIps.Where(c => c.AddressFamily == AddressFamily.InterNetworkV6))
            {
                eps.Add(new IPEndPoint(item, tunnelTransportInfo.Remote.Local.Port));
                eps.Add(new IPEndPoint(item, tunnelTransportInfo.Remote.Remote.Port));
                eps.Add(new IPEndPoint(item, tunnelTransportInfo.Remote.Remote.Port + 1));
            }
            //本机有V6
            bool hasV6 = tunnelTransportInfo.Local.LocalIps.Any(c => c.AddressFamily == AddressFamily.InterNetworkV6);
            //本机的局域网ip和外网ip
            List<IPAddress> localLocalIps = tunnelTransportInfo.Local.LocalIps.Concat(new List<IPAddress> { tunnelTransportInfo.Local.Remote.Address }).ToList();
            eps = eps
                //对方是V6，本机也得有V6
                .Where(c => (c.AddressFamily == AddressFamily.InterNetworkV6 && hasV6) || c.AddressFamily == AddressFamily.InterNetwork)
                //端口和本机端口一样，那不应该是换回地址
                .Where(c => (c.Port == tunnelTransportInfo.Local.Local.Port && c.Address.Equals(IPAddress.Loopback)) == false)
                //端口和本机端口一样。那不应该是本机的IP
                .Where(c => (c.Port == tunnelTransportInfo.Local.Local.Port && localLocalIps.Any(d => d.Equals(c.Address))) == false)
                .ToList();

            tunnelTransportInfo.RemoteEndPoints = eps;
        }
    }
}
