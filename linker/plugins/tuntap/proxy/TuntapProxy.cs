using linker.client.config;
using linker.config;
using linker.plugins.relay;
using linker.plugins.tuntap.vea;
using linker.tunnel;
using linker.tunnel.connection;
using linker.tunnel.proxy;
using linker.libs;
using linker.libs.extends;
using linker.libs.socks5;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace linker.plugins.tuntap.proxy
{
    public sealed class TuntapProxy : TunnelProxy
    {
        private readonly TunnelTransfer tunnelTransfer;
        private readonly RelayTransfer relayTransfer;
        private readonly RunningConfig runningConfig;
        private readonly FileConfig config;

        private IPEndPoint proxyEP;

        public override IPAddress UdpBindAdress { get; set; }

        private uint[] maskValues = Array.Empty<uint>();
        private readonly ConcurrentDictionary<uint, string> dic = new ConcurrentDictionary<uint, string>();
        private readonly ConcurrentDictionary<uint, IPAddress> hostipCic = new ConcurrentDictionary<uint, IPAddress>();
        private readonly ConcurrentDictionary<string, ITunnelConnection> connections = new ConcurrentDictionary<string, ITunnelConnection>();
        private readonly ConcurrentDictionary<string, SemaphoreSlim> dicLocks = new ConcurrentDictionary<string, SemaphoreSlim>();
        private ConcurrentDictionary<IPEndPoint, BroadcastCacheInfo> broadcastDic = new(new IPEndpointComparer());

        public TuntapProxy(TunnelTransfer tunnelTransfer, RelayTransfer relayTransfer, RunningConfig runningConfig, FileConfig config)
        {
            this.tunnelTransfer = tunnelTransfer;
            this.relayTransfer = relayTransfer;
            this.runningConfig = runningConfig;
            this.config = config;

            //监听打洞连接成功
            tunnelTransfer.SetConnectedCallback("tuntap", OnConnected);
            //监听中继连接成功
            relayTransfer.SetConnectedCallback("tuntap", OnConnected);
        }
        public void Start()
        {
            if (proxyEP != null)
            {
                Stop(proxyEP.Port);
            }
            Start(new IPEndPoint(IPAddress.Any, 0), runningConfig.Data.Tuntap.BufferSize);
            proxyEP = new IPEndPoint(IPAddress.Any, LocalEndpoint.Port);

            ClearTask();
        }

        /// <summary>
        /// 将隧道拦截成功的连接对象缓存起来，下次有连接需要隧道就直接拿，不用再去打洞或中继
        /// </summary>
        /// <param name="connection"></param>
        private void OnConnected(ITunnelConnection connection)
        {
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Warning($"tuntap add connection {connection.GetHashCode()} {connection.ToJson()}");
            if (connections.TryGetValue(connection.RemoteMachineId, out ITunnelConnection connectionOld))
            {
                LoggerHelper.Instance.Error($"new tunnel del {connection.Equals(connectionOld)}->{connectionOld.GetHashCode()}:{connectionOld.IPEndPoint}->{connection.GetHashCode()}:{connection.IPEndPoint}");
                //connectionOld?.Dispose();
            }
            connections.AddOrUpdate(connection.RemoteMachineId, connection, (a, b) => connection);
            BindConnectionReceive(connection);
        }

        /// <summary>
        /// 设置IP，等下有连接进来，用IP匹配，才能知道这个连接是要连谁
        /// </summary>
        /// <param name="ips"></param>
        public void SetIPs(List<TuntapVeaLanIPAddressList> ips)
        {
            dic.Clear();
            foreach (var item in ips)
            {
                foreach (var ip in item.IPS)
                {
                    dic.AddOrUpdate(ip.NetWork, item.MachineId, (a, b) => item.MachineId);
                }
            }
            maskValues = ips.SelectMany(c => c.IPS.Select(c => c.MaskValue)).Distinct().OrderBy(c => c).ToArray();
            UdpBindAdress = runningConfig.Data.Tuntap.IP;
        }
        public void SetIP(string machineId, uint ip)
        {
            dic.AddOrUpdate(ip, machineId, (a, b) => machineId);

            UdpBindAdress = runningConfig.Data.Tuntap.IP;
        }
        public void SetHostIP(uint ip, IPAddress hostip)
        {
            hostipCic.AddOrUpdate(ip, hostip, (a, b) => hostip);
        }

        /// <summary>
        /// tcp连接隧道
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        protected override async ValueTask<bool> ConnectTunnelConnection(AsyncUserToken token)
        {
            int length = await token.Socket.ReceiveAsync(token.Buffer.AsMemory(), SocketFlags.None);
            if (length == 0)
            {
                return true;
            }
            token.Proxy.Data = token.Buffer.AsMemory(0, length);

            token.Proxy.TargetEP = null;

            //步骤，request
            token.Proxy.Rsv = (byte)Socks5EnumStep.Request;
            if (await ReceiveCommandData(token) == false)
            {
                return true;
            }
            await token.Socket.SendAsync(new byte[] { 0x05, 0x00 }).ConfigureAwait(false);

            //步骤，command
            token.Proxy.Data = Helper.EmptyArray;
            token.Proxy.Rsv = (byte)Socks5EnumStep.Command;
            if (await ReceiveCommandData(token).ConfigureAwait(false) == false)
            {
                return true;
            }
            Socks5EnumRequestCommand command = (Socks5EnumRequestCommand)token.Proxy.Data.Span[1];

            //获取远端地址
            ReadOnlyMemory<byte> ipArray = Socks5Parser.GetRemoteEndPoint(token.Proxy.Data, out Socks5EnumAddressType addressType, out ushort port, out int index);
            token.Proxy.Data = token.Proxy.Data.Slice(index);
            token.Proxy.TargetEP = new IPEndPoint(new IPAddress(ipArray.Span), port);
            uint targetIP = BinaryPrimitives.ReadUInt32BigEndian(ipArray.Span);

            //不支持域名 和 IPV6
            if (addressType == Socks5EnumAddressType.Domain || addressType == Socks5EnumAddressType.IPV6)
            {
                byte[] response1 = Socks5Parser.MakeConnectResponse(new IPEndPoint(IPAddress.Any, 0), (byte)Socks5EnumResponseCommand.AddressNotAllow);
                await token.Socket.SendAsync(response1.AsMemory()).ConfigureAwait(false);
                return true;
            }

            //是UDP中继，不做连接操作，等UDP数据过去的时候再绑定
            if (targetIP == 0 || command == Socks5EnumRequestCommand.UdpAssociate)
            {
                await token.Socket.SendAsync(Socks5Parser.MakeConnectResponse(new IPEndPoint(IPAddress.Any, proxyEP.Port), (byte)Socks5EnumResponseCommand.ConnecSuccess).AsMemory()).ConfigureAwait(false);
                return false;
            }

            ReplaceTargetIP(token.Proxy.TargetEP, targetIP);
            token.Connection = await ConnectTunnel(targetIP).ConfigureAwait(false);

            Socks5EnumResponseCommand resp = token.Connection != null && token.Connection.Connected ? Socks5EnumResponseCommand.ConnecSuccess : Socks5EnumResponseCommand.NetworkError;
            byte[] response = Socks5Parser.MakeConnectResponse(new IPEndPoint(IPAddress.Any, 0), (byte)resp);
            await token.Socket.SendAsync(response.AsMemory()).ConfigureAwait(false);

            return true;
        }
        /// <summary>
        /// udp连接隧道
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        protected override async ValueTask ConnectTunnelConnection(AsyncUserUdpToken token)
        {
            ReadOnlyMemory<byte> ipArray = Socks5Parser.GetRemoteEndPoint(token.Proxy.Data, out Socks5EnumAddressType addressType, out ushort port, out int index);
            if (addressType == Socks5EnumAddressType.IPV6)
            {
                return;
            }

            uint targetIP = BinaryPrimitives.ReadUInt32BigEndian(ipArray.Span);
            token.Proxy.TargetEP = new IPEndPoint(new IPAddress(ipArray.Span), port);
            IPEndPoint target = new IPEndPoint(new IPAddress(ipArray.Span), port);

            //解析出udp包的数据部分
            token.Proxy.Data = Socks5Parser.GetUdpData(token.Proxy.Data);

            //是广播消息
            if (ipArray.Span[3] == 255 || token.Proxy.TargetEP.Address.GetIsBroadcastAddress())
            {
                token.Proxy.TargetEP.Address = IPAddress.Loopback;

                //我们替换了IP，但是等下要回复UDP数据给socks5时，要用原来的IP，所以缓存一下，等下回复要用
                if (broadcastDic.TryGetValue(token.Proxy.SourceEP, out BroadcastCacheInfo cache) == false)
                {
                    cache = new BroadcastCacheInfo { TargetEP = target };
                    broadcastDic.TryAdd(token.Proxy.SourceEP, cache);
                }
                cache.LastTime = Environment.TickCount64;

                //广播不去连接，直接获取已经有的所有连接
                token.Connections = connections.Values.ToList();
            }
            else
            {
                ReplaceTargetIP(token.Proxy.TargetEP, targetIP);
                token.Connection = await ConnectTunnel(targetIP).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// //在docker内，我们不应该直接访问自己的虚拟IP，而是去访问宿主机的IP
        /// </summary>
        /// <param name="targetEP"></param>
        /// <param name="targetIP"></param>
        private void ReplaceTargetIP(IPEndPoint targetEP, uint targetIP)
        {
            if (hostipCic.TryGetValue(targetIP, out IPAddress hostip) && hostip.Equals(IPAddress.Any) == false)
            {
                targetEP.Address = hostip;
            }
        }

        /// <summary>
        /// udp回复消息自定义处理，因为socks5，要打包一些格式才能返回
        /// </summary>
        /// <param name="token"></param>
        /// <param name="asyncUserUdpToken"></param>
        /// <returns></returns>
        protected override async ValueTask<bool> ConnectionReceiveUdp(AsyncUserTunnelToken token, AsyncUserUdpToken asyncUserUdpToken)
        {
            IPEndPoint target = token.Proxy.TargetEP;
            if (broadcastDic.TryGetValue(token.Proxy.SourceEP, out BroadcastCacheInfo cache))
            {
                target = cache.TargetEP;
                cache.LastTime = Environment.TickCount64;
            }
            byte[] data = Socks5Parser.MakeUdpResponse(target, token.Proxy.Data, out int length);
            try
            {
                await asyncUserUdpToken.SourceSocket.SendToAsync(data.AsMemory(0, length), token.Proxy.SourceEP).ConfigureAwait(false);
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
                Socks5Parser.Return(data);
            }
            return true;
        }
        private void ClearTask()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    long time = Environment.TickCount64;
                    foreach (var item in broadcastDic.Where(c => time - c.Value.LastTime > 30000).Select(c => c.Key).ToList())
                    {
                        broadcastDic.TryRemove(item, out _);
                    };
                    await Task.Delay(30000);
                }
            });
        }


        SemaphoreSlim slimGlobal = new SemaphoreSlim(1);
        /// <summary>
        /// 打洞或者中继
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        private async ValueTask<ITunnelConnection> ConnectTunnel(uint ip)
        {
            //直接按IP查找
            if (dic.TryGetValue(ip, out string machineId))
            {
                return await ConnectTunnel(machineId).ConfigureAwait(false);
            }

            //匹配掩码查找
            for (int i = 0; i < maskValues.Length; i++)
            {
                uint network = ip & maskValues[i];
                if (dic.TryGetValue(network, out machineId))
                {
                    return await ConnectTunnel(machineId).ConfigureAwait(false);
                }
            }

            return null;

        }
        /// <summary>
        /// 打洞或者中继
        /// </summary>
        /// <param name="machineId"></param>
        /// <returns></returns>
        private async ValueTask<ITunnelConnection> ConnectTunnel(string machineId)
        {
            if (config.Data.Client.Id == machineId)
            {
                return null;
            }

            if (connections.TryGetValue(machineId, out ITunnelConnection connection) && connection.Connected)
            {
                return connection;
            }

            await slimGlobal.WaitAsync().ConfigureAwait(false);
            if (dicLocks.TryGetValue(machineId, out SemaphoreSlim slim) == false)
            {
                slim = new SemaphoreSlim(1);
                dicLocks.TryAdd(machineId, slim);
            }
            slimGlobal.Release();

            await slim.WaitAsync().ConfigureAwait(false);

            try
            {
                if (connections.TryGetValue(machineId, out connection) && connection.Connected)
                {
                    return connection;
                }

                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG) LoggerHelper.Instance.Debug($"tuntap tunnel to {machineId}");

                connection = await tunnelTransfer.ConnectAsync(machineId, "tuntap").ConfigureAwait(false);
                if (connection != null)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG) LoggerHelper.Instance.Debug($"tuntap tunnel success,{connection.ToString()}");
                }
                if (connection == null)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG) LoggerHelper.Instance.Debug($"tuntap relay to {machineId}");

                    connection = await relayTransfer.ConnectAsync(config.Data.Client.Id, machineId, "tuntap").ConfigureAwait(false);
                    if (connection != null)
                    {
                        //tunnelTransfer.StartBackground(machineId, "tuntap");
                        if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG) LoggerHelper.Instance.Debug($"tuntap relay success,{connection.ToString()}");
                    }
                }
                if (connection != null)
                {
                    connections.AddOrUpdate(machineId, connection, (a, b) => connection);
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                slim.Release();
            }
            return connection;
        }

        /// <summary>
        /// 接收socks5完整数据包
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task<bool> ReceiveCommandData(AsyncUserToken token)
        {
            int totalLength = token.Proxy.Data.Length;
            EnumProxyValidateDataResult validate = ValidateData(token.Proxy);
            //太短
            while ((validate & EnumProxyValidateDataResult.TooShort) == EnumProxyValidateDataResult.TooShort)
            {
                totalLength += await token.Socket.ReceiveAsync(token.Buffer.AsMemory(totalLength), SocketFlags.None).ConfigureAwait(false);
                token.Proxy.Data = token.Buffer.AsMemory(0, totalLength);
                validate = ValidateData(token.Proxy);
            }

            //不短，又不相等，直接关闭连接
            if ((validate & EnumProxyValidateDataResult.Equal) != EnumProxyValidateDataResult.Equal)
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// 验证socks5数据包完整性
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        private EnumProxyValidateDataResult ValidateData(ProxyInfo info)
        {
            return (Socks5EnumStep)info.Rsv switch
            {
                Socks5EnumStep.Request => Socks5Parser.ValidateRequestData(info.Data),
                Socks5EnumStep.Command => Socks5Parser.ValidateCommandData(info.Data),
                Socks5EnumStep.Auth => Socks5Parser.ValidateAuthData(info.Data, Socks5EnumAuthType.Password),
                Socks5EnumStep.Forward => EnumProxyValidateDataResult.Equal,
                Socks5EnumStep.ForwardUdp => EnumProxyValidateDataResult.Equal,
                _ => EnumProxyValidateDataResult.Equal
            };
        }

        public ConcurrentDictionary<string, ITunnelConnection> GetConnections()
        {
            return connections;
        }
        public void RemoveConnection(string machineId)
        {
            if (connections.TryRemove(machineId, out ITunnelConnection _connection))
            {
                try
                {
                    _connection.Dispose();
                }
                catch (Exception)
                {
                }
            }
        }

    }

    public sealed class BroadcastCacheInfo
    {
        public IPEndPoint TargetEP { get; set; }
        public long LastTime { get; set; } = Environment.TickCount64;

    }

    public sealed class IPEndpointComparer : IEqualityComparer<IPEndPoint>
    {
        public bool Equals(IPEndPoint x, IPEndPoint y)
        {
            return x.Equals(y);
        }
        public int GetHashCode(IPEndPoint obj)
        {
            return obj.GetHashCode();
        }
    }
}
