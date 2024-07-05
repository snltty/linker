using linker.client.config;
using linker.plugins.sforward.config;
using linker.plugins.sforward.validator;
using linker.plugins.signin.messenger;
using linker.server;
using MemoryPack;
using linker.plugins.sforward.proxy;
using linker.config;

namespace linker.plugins.sforward.messenger
{
    public sealed class SForwardServerMessenger : IMessenger
    {

        private readonly SForwardProxy proxy;
        private readonly ISForwardServerCahing sForwardServerCahing;
        private readonly MessengerSender sender;
        private readonly SignCaching signCaching;
        private readonly ConfigWrap configWrap;
        private readonly IValidator validator;

        public SForwardServerMessenger(SForwardProxy proxy, ISForwardServerCahing sForwardServerCahing, MessengerSender sender, SignCaching signCaching, ConfigWrap configWrap, IValidator validator)
        {
            this.proxy = proxy;
            proxy.WebConnect = WebConnect;
            proxy.TunnelConnect = TunnelConnect;
            proxy.UdpConnect = UdpConnect;
            this.sForwardServerCahing = sForwardServerCahing;
            this.sender = sender;
            this.signCaching = signCaching;
            this.configWrap = configWrap;
            this.validator = validator;
        }

        [MessengerId((ushort)SForwardMessengerIds.Add)]
        public void Add(IConnection connection)
        {
            SForwardAddInfo sForwardAddInfo = MemoryPackSerializer.Deserialize<SForwardAddInfo>(connection.ReceiveRequestWrap.Payload.Span);
            SForwardAddResultInfo result = new SForwardAddResultInfo { Success = true, BufferSize = configWrap.Data.Server.SForward.BufferSize };
            try
            {
                if (validator.Valid(connection, sForwardAddInfo, out string error) == false)
                {
                    result.Success = false;
                    result.Message = error;
                    return;
                }

                if (string.IsNullOrWhiteSpace(sForwardAddInfo.Domain) == false)
                {
                    if (sForwardServerCahing.TryAdd(sForwardAddInfo.Domain, connection.Id) == false)
                    {
                        result.Success = false;
                        result.Message = $"domain 【{sForwardAddInfo.Domain}】 already exists";
                    }
                    else
                    {
                        result.Message = $"domain 【{sForwardAddInfo.Domain}】 add success";
                    }
                    return;
                }
                if (sForwardAddInfo.RemotePort > 0)
                {
                    if (sForwardServerCahing.TryAdd(sForwardAddInfo.RemotePort, connection.Id) == false)
                    {

                        result.Success = false;
                        result.Message = $"port 【{sForwardAddInfo.RemotePort}】 already exists";
                    }
                    else
                    {
                        string msg = proxy.Start(sForwardAddInfo.RemotePort, false, configWrap.Data.Server.SForward.BufferSize);
                        if (string.IsNullOrWhiteSpace(msg) == false)
                        {
                            result.Success = false;
                            result.Message = $"port 【{sForwardAddInfo.RemotePort}】 add fail : {msg}";
                            sForwardServerCahing.TryRemove(sForwardAddInfo.RemotePort, connection.Id, out _);
                        }
                        else
                        {
                            result.Message = $"port 【{sForwardAddInfo.RemotePort}】 add success";
                        }
                    }
                    return;
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"sforward fail : {ex.Message}";
            }
            finally
            {
                connection.Write(MemoryPackSerializer.Serialize(result));
            }

        }

        [MessengerId((ushort)SForwardMessengerIds.Remove)]
        public void Remove(IConnection connection)
        {
            SForwardAddInfo sForwardAddInfo = MemoryPackSerializer.Deserialize<SForwardAddInfo>(connection.ReceiveRequestWrap.Payload.Span);
            SForwardAddResultInfo result = new SForwardAddResultInfo { Success = true };

            try
            {
                if (validator.Valid(connection, sForwardAddInfo, out string error) == false)
                {
                    result.Success = false;
                    result.Message = error;
                    return;
                }

                if (string.IsNullOrWhiteSpace(sForwardAddInfo.Domain) == false)
                {
                    if (sForwardServerCahing.TryRemove(sForwardAddInfo.Domain, connection.Id, out _) == false)
                    {
                        result.Success = false;
                        result.Message = $"domain 【{sForwardAddInfo.Domain}】 remove fail";
                    }
                    else
                    {
                        result.Message = $"domain 【{sForwardAddInfo.Domain}】 remove success";
                    }
                    return;
                }

                if (sForwardAddInfo.RemotePort > 0)
                {
                    if (sForwardServerCahing.TryRemove(sForwardAddInfo.RemotePort, connection.Id, out _) == false)
                    {
                        result.Success = false;
                        result.Message = $"port 【{sForwardAddInfo.RemotePort}】 remove fail";
                    }
                    else
                    {
                        proxy.Stop(sForwardAddInfo.RemotePort);
                        result.Message = $"port 【{sForwardAddInfo.RemotePort}】 remove success";
                    }
                    return;
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"sforward fail : {ex.Message}";
            }
            finally
            {
                connection.Write(MemoryPackSerializer.Serialize(result));
            }
        }

        private async Task<bool> WebConnect(string host, int port, ulong id)
        {
            if (sForwardServerCahing.TryGet(host, out string machineId) && signCaching.TryGet(machineId, out SignCacheInfo sign) && sign.Connected)
            {
                return await sender.SendOnly(new MessageRequestWrap
                {
                    Connection = sign.Connection,
                    MessengerId = (ushort)SForwardMessengerIds.Proxy,
                    Payload = MemoryPackSerializer.Serialize(new SForwardProxyInfo { Domain = host, RemotePort = port, Id = id, BufferSize= configWrap.Data.Server.SForward.BufferSize })
                }).ConfigureAwait(false);
            }
            return false;
        }
        private async Task<bool> TunnelConnect(int port, ulong id)
        {
            if (sForwardServerCahing.TryGet(port, out string machineId) && signCaching.TryGet(machineId, out SignCacheInfo sign) && sign.Connected)
            {
                return await sender.SendOnly(new MessageRequestWrap
                {
                    Connection = sign.Connection,
                    MessengerId = (ushort)SForwardMessengerIds.Proxy,
                    Payload = MemoryPackSerializer.Serialize(new SForwardProxyInfo { RemotePort = port, Id = id, BufferSize = configWrap.Data.Server.SForward.BufferSize })
                }).ConfigureAwait(false);
            }
            return false;
        }
        private async Task<bool> UdpConnect(int port, ulong id)
        {
            if (sForwardServerCahing.TryGet(port, out string machineId) && signCaching.TryGet(machineId, out SignCacheInfo sign) && sign.Connected)
            {
                return await sender.SendOnly(new MessageRequestWrap
                {
                    Connection = sign.Connection,
                    MessengerId = (ushort)SForwardMessengerIds.ProxyUdp,
                    Payload = MemoryPackSerializer.Serialize(new SForwardProxyInfo { RemotePort = port, Id = id, BufferSize = configWrap.Data.Server.SForward.BufferSize })
                }).ConfigureAwait(false);
            }
            return false;
        }
    }

    public sealed class SForwardClientMessenger : IMessenger
    {
        private readonly SForwardProxy proxy;
        private readonly RunningConfig runningConfig;

        public SForwardClientMessenger(SForwardProxy proxy, RunningConfig runningConfig)
        {
            this.proxy = proxy;
            this.runningConfig = runningConfig;
        }

        [MessengerId((ushort)SForwardMessengerIds.Proxy)]
        public void Proxy(IConnection connection)
        {
            SForwardProxyInfo sForwardProxyInfo = MemoryPackSerializer.Deserialize<SForwardProxyInfo>(connection.ReceiveRequestWrap.Payload.Span);

            if (string.IsNullOrWhiteSpace(sForwardProxyInfo.Domain) == false)
            {
                SForwardInfo sForwardInfo = runningConfig.Data.SForwards.FirstOrDefault(c => c.Domain == sForwardProxyInfo.Domain);
                if (sForwardInfo != null)
                {
                    _ = proxy.OnConnectTcp(sForwardProxyInfo.BufferSize,sForwardProxyInfo.Id, new System.Net.IPEndPoint(connection.Address.Address, sForwardProxyInfo.RemotePort), sForwardInfo.LocalEP);
                }
            }
            else if (sForwardProxyInfo.RemotePort > 0)
            {
                SForwardInfo sForwardInfo = runningConfig.Data.SForwards.FirstOrDefault(c => c.RemotePort == sForwardProxyInfo.RemotePort);
                if (sForwardInfo != null)
                {
                    _ = proxy.OnConnectTcp(sForwardProxyInfo.BufferSize, sForwardProxyInfo.Id, new System.Net.IPEndPoint(connection.Address.Address, sForwardProxyInfo.RemotePort), sForwardInfo.LocalEP);
                }
            }
        }
        [MessengerId((ushort)SForwardMessengerIds.ProxyUdp)]
        public void ProxyUdp(IConnection connection)
        {
            SForwardProxyInfo sForwardProxyInfo = MemoryPackSerializer.Deserialize<SForwardProxyInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (sForwardProxyInfo.RemotePort > 0)
            {
                SForwardInfo sForwardInfo = runningConfig.Data.SForwards.FirstOrDefault(c => c.RemotePort == sForwardProxyInfo.RemotePort);
                if (sForwardInfo != null)
                {
                    _ = proxy.OnConnectUdp(sForwardProxyInfo.BufferSize, sForwardProxyInfo.Id, new System.Net.IPEndPoint(connection.Address.Address, sForwardProxyInfo.RemotePort), sForwardInfo.LocalEP);
                }
            }
        }
    }
}
