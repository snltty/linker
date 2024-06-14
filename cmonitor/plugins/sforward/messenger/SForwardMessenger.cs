using cmonitor.client.config;
using cmonitor.plugins.sforward.config;
using cmonitor.plugins.sforward.validator;
using cmonitor.plugins.signin.messenger;
using cmonitor.server;
using MemoryPack;

namespace cmonitor.plugins.sforward.messenger
{
    public sealed class SForwardServerMessenger : IMessenger
    {

        private readonly SForwardProxy proxy;
        private readonly ISForwardServerCahing sForwardServerCahing;
        private readonly MessengerSender sender;
        private readonly SignCaching signCaching;

        private readonly IValidator validator;

        public SForwardServerMessenger(SForwardProxy proxy, ISForwardServerCahing sForwardServerCahing, MessengerSender sender, SignCaching signCaching, IValidator validator)
        {
            this.proxy = proxy;
            proxy.WebConnect = WebConnect;
            proxy.TunnelConnect = TunnelConnect;
            this.sForwardServerCahing = sForwardServerCahing;
            this.sender = sender;
            this.signCaching = signCaching;
            this.validator = validator;
        }

        [MessengerId((ushort)SForwardMessengerIds.Add)]
        public void Add(IConnection connection)
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
                        string msg = proxy.Start(sForwardAddInfo.RemotePort, false);
                        if (string.IsNullOrWhiteSpace(msg) == false)
                        {
                            result.Success = false;
                            result.Message = $"port 【{sForwardAddInfo.RemotePort}】 add fail : {msg}";
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


        [MessengerId((ushort)SForwardMessengerIds.SecretKeyForward)]
        public async void SecretKeyForward(IConnection connection)
        {
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache))
            {
                var caches = signCaching.Get(cache.GroupId);

                foreach (var item in caches)
                {
                    await sender.SendOnly(new MessageRequestWrap
                    {
                        Connection = item.Connection,
                        MessengerId = (ushort)SForwardMessengerIds.SecretKey,
                        Payload = connection.ReceiveRequestWrap.Payload
                    });
                }
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
                    Payload = MemoryPackSerializer.Serialize(new SForwardProxyInfo { Domain = host, RemotePort = port, Id = id })
                });
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
                    Payload = MemoryPackSerializer.Serialize(new SForwardProxyInfo { RemotePort = port, Id = id })
                });
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
                    _ = proxy.OnConnectTcp(sForwardProxyInfo.Id, new System.Net.IPEndPoint(connection.Address.Address, sForwardProxyInfo.RemotePort), sForwardInfo.LocalEP);
                }
            }
            else if (sForwardProxyInfo.RemotePort > 0)
            {
                SForwardInfo sForwardInfo = runningConfig.Data.SForwards.FirstOrDefault(c => c.RemotePort == sForwardProxyInfo.RemotePort);
                if (sForwardInfo != null)
                {
                    _ = proxy.OnConnectTcp(sForwardProxyInfo.Id, new System.Net.IPEndPoint(connection.Address.Address, sForwardProxyInfo.RemotePort), sForwardInfo.LocalEP);
                }
            }
        }

        [MessengerId((ushort)SForwardMessengerIds.SecretKey)]
        public void SecretKey(IConnection connection)
        {
            string sForwardSecretKey = MemoryPackSerializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            runningConfig.Data.SForwardSecretKey = sForwardSecretKey;
            runningConfig.Data.Update();
        }
    }
}
