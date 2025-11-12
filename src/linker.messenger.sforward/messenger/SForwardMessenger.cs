using linker.plugins.sforward.proxy;
using System.Net;
using linker.libs;
using linker.messenger.signin;
using linker.messenger.sforward.server.validator;
using linker.messenger.sforward.server;
using linker.messenger.sforward.client;
using System.Net.Sockets;
using linker.libs.timer;

namespace linker.messenger.sforward.messenger
{
    /// <summary>
    /// 穿透服务端
    /// </summary>
    public sealed class SForwardServerMessenger : IMessenger
    {

        private readonly SForwardProxy proxy;
        private readonly ISForwardServerCahing sForwardServerCahing;
        private readonly IMessengerSender sender;
        private readonly SignInServerCaching signCaching;
        private readonly SForwardValidatorTransfer validator;
        private readonly ISerializer serializer;
        private readonly SForwardServerMasterTransfer sForwardServerMasterTransfer;
        private readonly SForwardServerNodeTransfer sForwardServerNodeTransfer;
        private readonly ISignInServerStore signInServerStore;

        public SForwardServerMessenger(SForwardProxy proxy, ISForwardServerCahing sForwardServerCahing, IMessengerSender sender,
            SignInServerCaching signCaching, SForwardValidatorTransfer validator, ISerializer serializer,
            SForwardServerMasterTransfer sForwardServerMasterTransfer, SForwardServerNodeTransfer sForwardServerNodeTransfer,
            ISignInServerStore signInServerStore)
        {
            this.proxy = proxy;
            proxy.WebConnect = WebConnect;
            proxy.TunnelConnect = TunnelConnect;
            proxy.UdpConnect = UdpConnect;
            this.sForwardServerCahing = sForwardServerCahing;
            this.sender = sender;
            this.signCaching = signCaching;
            this.validator = validator;
            this.serializer = serializer;
            this.sForwardServerMasterTransfer = sForwardServerMasterTransfer;
            this.sForwardServerNodeTransfer = sForwardServerNodeTransfer;
            this.signInServerStore = signInServerStore;

            ClearTask();
        }

        [MessengerId((ushort)SForwardMessengerIds.Nodes)]
        public async Task Nodes(IConnection connection)
        {
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) == false)
            {
                connection.Write(serializer.Serialize(new List<SForwardServerNodeReportInfo>()));
                return;
            }

            var nodes = await sForwardServerMasterTransfer.GetNodes(cache.Super, cache.UserId, cache.MachineId);

            connection.Write(serializer.Serialize(nodes));
        }
        [MessengerId((ushort)SForwardMessengerIds.NodeReport)]
        public void NodeReport(IConnection connection)
        {
            try
            {
                SForwardServerNodeReportInfo info = serializer.Deserialize<SForwardServerNodeReportInfo>(connection.ReceiveRequestWrap.Payload.Span);
                sForwardServerMasterTransfer.SetNodeReport(connection, info);
            }
            catch (Exception)
            {
            }

            connection.Write(serializer.Serialize(VersionHelper.Version));
        }
        [MessengerId((ushort)SForwardMessengerIds.Edit)]
        public void Edit(IConnection connection)
        {
            SForwardServerNodeUpdateInfo info = serializer.Deserialize<SForwardServerNodeUpdateInfo>(connection.ReceiveRequestWrap.Payload.Span);
            sForwardServerNodeTransfer.Edit(info);
        }
        [MessengerId((ushort)SForwardMessengerIds.EditForward)]
        public async Task EditForward188(IConnection connection)
        {
            SForwardServerNodeUpdateWrapInfo info = serializer.Deserialize<SForwardServerNodeUpdateWrapInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) && cache.Super)
            {
                await sForwardServerMasterTransfer.Edit(info.Info).ConfigureAwait(false);
                connection.Write(Helper.TrueArray);
            }
            else
            {
                connection.Write(Helper.FalseArray);
            }
        }

        [MessengerId((ushort)SForwardMessengerIds.Exit)]
        public void Exit(IConnection connection)
        {
            sForwardServerNodeTransfer.Exit();
        }
        [MessengerId((ushort)SForwardMessengerIds.ExitForward)]
        public async Task ExitForward(IConnection connection)
        {
            string id = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) && cache.Super)
            {
                await sForwardServerMasterTransfer.Exit(id).ConfigureAwait(false);
                connection.Write(Helper.TrueArray);
            }
            else
            {
                connection.Write(Helper.FalseArray);
            }
        }

        [MessengerId((ushort)SForwardMessengerIds.Update)]
        public void Update(IConnection connection)
        {
            sForwardServerNodeTransfer.Update(serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span));
        }
        [MessengerId((ushort)SForwardMessengerIds.UpdateForward)]
        public async Task UpdateForward(IConnection connection)
        {
            KeyValuePair<string, string> info = serializer.Deserialize<KeyValuePair<string, string>>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) && cache.Super)
            {
                await sForwardServerMasterTransfer.Update(info.Key, info.Value).ConfigureAwait(false);
                connection.Write(Helper.TrueArray);
            }
            else
            {
                connection.Write(Helper.FalseArray);
            }
        }

        /// <summary>
        /// 添加穿透
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)SForwardMessengerIds.Add)]
        public async Task Add(IConnection connection)
        {
            SForwardAddInfo sForwardAddInfo = serializer.Deserialize<SForwardAddInfo>(connection.ReceiveRequestWrap.Payload.Span);
            SForwardAddResultInfo result = new SForwardAddResultInfo { Success = true, BufferSize = 3 };
            try
            {
                if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) == false)
                {
                    result.Success = false;
                    result.Message = "need sign in";
                    return;
                }
                SForwardAddInfo191 sForwardAddInfo191 = new SForwardAddInfo191
                {
                    Domain = sForwardAddInfo.Domain,
                    RemotePort = sForwardAddInfo.RemotePort,
                    MachineId = cache.MachineId,
                    GroupId = cache.GroupId
                };
                string error = await validator.Validate(cache, sForwardAddInfo191).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(error) == false)
                {
                    result.Success = false;
                    result.Message = error;
                    return;
                }
                Add(sForwardAddInfo, cache.MachineId, cache.GroupId, result, sForwardAddInfo191.Super, sForwardAddInfo191.Bandwidth);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"sforward fail : {ex.Message}";
                LoggerHelper.Instance.Error(result.Message);
            }
            finally
            {
                connection.Write(serializer.Serialize(result));
            }

        }
        [MessengerId((ushort)SForwardMessengerIds.Add191)]
        public void Add191(IConnection connection)
        {
            SForwardAddInfo191 sForwardAddInfo = serializer.Deserialize<SForwardAddInfo191>(connection.ReceiveRequestWrap.Payload.Span);
            SForwardAddResultInfo result = new SForwardAddResultInfo { Success = true, BufferSize = 3 };
            try
            {
                Add(sForwardAddInfo, result);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"sforward fail : {ex.Message}";
                LoggerHelper.Instance.Error(result.Message);
            }
            finally
            {
                connection.Write(serializer.Serialize(result));
            }

        }
        [MessengerId((ushort)SForwardMessengerIds.AddForward191)]
        public async Task AddForward191(IConnection connection)
        {
            SForwardAddInfo191 sForwardAddInfo = serializer.Deserialize<SForwardAddInfo191>(connection.ReceiveRequestWrap.Payload.Span);
            SForwardAddResultInfo result = new SForwardAddResultInfo { Success = true, BufferSize = 3 };
            try
            {
                if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) == false)
                {
                    result.Success = false;
                    result.Message = "need sign in";
                    return;
                }
                string error = await validator.Validate(cache, sForwardAddInfo).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(error) == false)
                {
                    result.Success = false;
                    result.Message = error;
                    return;
                }

                sForwardAddInfo.GroupId = cache.GroupId;
                sForwardAddInfo.MachineId = cache.MachineId;
                result = await sForwardServerMasterTransfer.Add(sForwardAddInfo, cache);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"sforward fail : {ex.Message}";
                LoggerHelper.Instance.Error(result.Message);
            }
            finally
            {
                connection.Write(serializer.Serialize(result));
            }

        }
        [MessengerId((ushort)SForwardMessengerIds.Heart)]
        public void Heart(IConnection connection)
        {
            List<string> ids = serializer.Deserialize<List<string>>(connection.ReceiveRequestWrap.Payload.Span);
            connection.Write(serializer.Serialize(ids.Except(signCaching.GetOnline()).ToList()));
        }
        [MessengerId((ushort)SForwardMessengerIds.Hosts)]
        public void Hosts(IConnection connection)
        {
            connection.Write(serializer.Serialize(signInServerStore.Hosts));
        }

        /// <summary>
        /// 删除穿透
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)SForwardMessengerIds.Remove)]
        public void Remove(IConnection connection)
        {
            SForwardAddInfo sForwardAddInfo = serializer.Deserialize<SForwardAddInfo>(connection.ReceiveRequestWrap.Payload.Span);
            SForwardAddResultInfo result = new SForwardAddResultInfo { Success = true };

            try
            {
                if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) == false)
                {
                    result.Success = false;
                    result.Message = "need sign in";
                    return;
                }
                Remove(sForwardAddInfo, cache.MachineId, result);
            }
            catch (Exception)
            {
            }
            finally
            {
                connection.Write(serializer.Serialize(result));
            }
        }
        [MessengerId((ushort)SForwardMessengerIds.Remove191)]
        public void Remove191(IConnection connection)
        {
            SForwardAddInfo191 sForwardAddInfo = serializer.Deserialize<SForwardAddInfo191>(connection.ReceiveRequestWrap.Payload.Span);
            SForwardAddResultInfo result = new SForwardAddResultInfo { Success = true };

            try
            {
                Remove(sForwardAddInfo, result);
            }
            catch (Exception)
            {
            }
            finally
            {
                connection.Write(serializer.Serialize(result));
            }
        }
        [MessengerId((ushort)SForwardMessengerIds.RemoveForward191)]
        public async Task RemoveForward191(IConnection connection)
        {
            SForwardAddInfo191 sForwardAddInfo = serializer.Deserialize<SForwardAddInfo191>(connection.ReceiveRequestWrap.Payload.Span);
            SForwardAddResultInfo result = new SForwardAddResultInfo { Success = true };

            try
            {
                if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) == false)
                {
                    result.Success = false;
                    result.Message = "need sign in";
                    return;
                }
                sForwardAddInfo.GroupId = cache.GroupId;
                sForwardAddInfo.MachineId = cache.MachineId;
                result = await sForwardServerMasterTransfer.Remove(sForwardAddInfo);
            }
            catch (Exception)
            {
            }
            finally
            {
                connection.Write(serializer.Serialize(result));
            }
        }


        public void Remove(SForwardAddInfo sForwardAddInfo, string machineId, SForwardAddResultInfo result)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sForwardAddInfo.Domain) == false)
                {
                    if (PortRange(sForwardAddInfo.Domain, out int min, out int max))
                    {
                        for (int port = min; port <= max; port++)
                        {
                            if (sForwardServerCahing.TryRemove(port, machineId, out _))
                            {
                                proxy.Stop(port);
                            }
                        }
                    }
                    else
                    {
                        sForwardServerCahing.TryRemove(sForwardAddInfo.Domain, machineId, out _);
                        proxy.RemoveHttp(sForwardAddInfo.Domain);
                        result.Message = $"domain 【{sForwardAddInfo.Domain}】 remove success";
                    }
                    return;
                }

                if (sForwardAddInfo.RemotePort > 0)
                {
                    sForwardServerCahing.TryRemove(sForwardAddInfo.RemotePort, machineId, out _);
                    proxy.Stop(sForwardAddInfo.RemotePort);
                    result.Message = $"port 【{sForwardAddInfo.RemotePort}】 remove success";
                    return;
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"sforward fail : {ex.Message}";
            }
        }
        public void Remove(SForwardAddInfo191 sForwardAddInfo, SForwardAddResultInfo result)
        {
            Remove((SForwardAddInfo)sForwardAddInfo, sForwardAddInfo.MachineId, result);
        }
        public void Add(SForwardAddInfo sForwardAddInfo, string machineId, string groupid, SForwardAddResultInfo result, bool super, double bandwidth)
        {
            try
            {
                //有域名，
                if (string.IsNullOrWhiteSpace(sForwardAddInfo.Domain) == false)
                {
                    //有可能是 端口范围，不是真的域名
                    if (PortRange(sForwardAddInfo.Domain, out int min, out int max))
                    {
                        for (int port = min; port <= max; port++)
                        {
                            if (sForwardServerCahing.TryAdd(port, machineId))
                            {
                                proxy.Stop(port);
                                result.Message = proxy.Start(port, 3, groupid, super, bandwidth);
                                if (string.IsNullOrWhiteSpace(result.Message) == false)
                                {
                                    LoggerHelper.Instance.Error(result.Message);
                                    sForwardServerCahing.TryRemove(port, machineId, out _);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (sForwardServerCahing.TryAdd(sForwardAddInfo.Domain, machineId) == false)
                        {
                            result.Success = false;
                            result.Message = $"domain 【{sForwardAddInfo.Domain}】 already exists";
                            LoggerHelper.Instance.Error(result.Message);
                        }
                        else
                        {

                            proxy.AddHttp(sForwardAddInfo.Domain, super, bandwidth);
                            result.Message = $"domain 【{sForwardAddInfo.Domain}】 add success";
                        }
                    }
                    return;
                }
                //如果是端口
                if (sForwardAddInfo.RemotePort > 0)
                {
                    if (sForwardServerCahing.TryAdd(sForwardAddInfo.RemotePort, machineId) == false)
                    {

                        result.Success = false;
                        result.Message = $"port 【{sForwardAddInfo.RemotePort}】 already exists";
                        LoggerHelper.Instance.Error(result.Message);
                    }
                    else
                    {
                        proxy.Stop(sForwardAddInfo.RemotePort);
                        string msg = proxy.Start(sForwardAddInfo.RemotePort, 3, groupid, super, bandwidth);
                        if (string.IsNullOrWhiteSpace(msg) == false)
                        {
                            result.Success = false;
                            result.Message = $"port 【{sForwardAddInfo.RemotePort}】 add fail : {msg}";
                            sForwardServerCahing.TryRemove(sForwardAddInfo.RemotePort, machineId, out _);
                            LoggerHelper.Instance.Error(result.Message);
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
        }
        public void Add(SForwardAddInfo191 sForwardAddInfo, SForwardAddResultInfo result)
        {
            Add((SForwardAddInfo)sForwardAddInfo, sForwardAddInfo.MachineId, sForwardAddInfo.GroupId, result, sForwardAddInfo.Super, sForwardAddInfo.Bandwidth);
        }
        private static bool PortRange(string str, out int min, out int max)
        {
            min = 0; max = 0;
            string[] arr = str.Split('/');
            return arr.Length == 2 && int.TryParse(arr[0], out min) && int.TryParse(arr[1], out max);
        }


        /// <summary>
        /// 获取对端的穿透记录
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)SForwardMessengerIds.GetForward)]
        public void GetForward(IConnection connection)
        {
            string machineId = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, machineId, out SignCacheInfo from, out SignCacheInfo to))
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;
                sender.SendReply(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)SForwardMessengerIds.Get,
                    Payload = connection.ReceiveRequestWrap.Payload
                }).ContinueWith(async (result) =>
                {
                    if (result.Result.Code == MessageResponeCodes.OK)
                    {
                        await sender.ReplyOnly(new MessageResponseWrap
                        {
                            Connection = connection,
                            Code = MessageResponeCodes.OK,
                            Payload = result.Result.Data,
                            RequestId = requestid
                        }, (ushort)SForwardMessengerIds.GetForward).ConfigureAwait(false);
                    }
                }).ConfigureAwait(false);
            }
        }
        /// <summary>
        /// 添加对端的穿透记录
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)SForwardMessengerIds.AddClientForward)]
        public async Task AddClientForward(IConnection connection)
        {
            SForwardAddForwardInfo info = serializer.Deserialize<SForwardAddForwardInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, info.MachineId, out SignCacheInfo from, out SignCacheInfo to))
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;
                await sender.SendReply(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)SForwardMessengerIds.AddClient,
                    Payload = serializer.Serialize(info.Data)
                }).ContinueWith(async (result) =>
                {
                    if (result.Result.Code == MessageResponeCodes.OK)
                    {
                        await sender.ReplyOnly(new MessageResponseWrap
                        {
                            Connection = connection,
                            Code = MessageResponeCodes.OK,
                            Payload = result.Result.Data,
                            RequestId = requestid
                        }, (ushort)SForwardMessengerIds.AddClientForward).ConfigureAwait(false);
                    }
                }).ConfigureAwait(false);
            }
        }
        [MessengerId((ushort)SForwardMessengerIds.AddClientForward191)]
        public async Task AddClientForward191(IConnection connection)
        {
            SForwardAddForwardInfo191 info = serializer.Deserialize<SForwardAddForwardInfo191>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, info.MachineId, out SignCacheInfo from, out SignCacheInfo to))
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;
                await sender.SendReply(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)SForwardMessengerIds.AddClient191,
                    Payload = serializer.Serialize(info.Data)
                }).ContinueWith(async (result) =>
                {
                    if (result.Result.Code == MessageResponeCodes.OK)
                    {
                        await sender.ReplyOnly(new MessageResponseWrap
                        {
                            Connection = connection,
                            Code = MessageResponeCodes.OK,
                            Payload = result.Result.Data,
                            RequestId = requestid
                        }, (ushort)SForwardMessengerIds.AddClientForward191).ConfigureAwait(false);
                    }
                }).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 删除对端的穿透记录
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)SForwardMessengerIds.RemoveClientForward)]
        public async Task RemoveClientForward(IConnection connection)
        {
            SForwardRemoveForwardInfo info = serializer.Deserialize<SForwardRemoveForwardInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, info.MachineId, out SignCacheInfo from, out SignCacheInfo to))
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;
                await sender.SendReply(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)SForwardMessengerIds.RemoveClient,
                    Payload = serializer.Serialize(info.Id)
                }).ContinueWith(async (result) =>
                {
                    if (result.Result.Code == MessageResponeCodes.OK)
                    {
                        await sender.ReplyOnly(new MessageResponseWrap
                        {
                            Connection = connection,
                            Code = MessageResponeCodes.OK,
                            Payload = result.Result.Data,
                            RequestId = requestid
                        }, (ushort)SForwardMessengerIds.RemoveClientForward).ConfigureAwait(false);
                    }
                }).ConfigureAwait(false);
            }
        }

        [MessengerId((ushort)SForwardMessengerIds.StartClientForward)]
        public async Task StartClientForward(IConnection connection)
        {
            SForwardRemoveForwardInfo info = serializer.Deserialize<SForwardRemoveForwardInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, info.MachineId, out SignCacheInfo from, out SignCacheInfo to))
            {
                await sender.SendOnly(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)SForwardMessengerIds.StartClient,
                    Payload = serializer.Serialize(info.Id)
                }).ConfigureAwait(false);
            }
        }
        [MessengerId((ushort)SForwardMessengerIds.StopClientForward)]
        public async Task StopClientForward(IConnection connection)
        {
            SForwardRemoveForwardInfo info = serializer.Deserialize<SForwardRemoveForwardInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, info.MachineId, out SignCacheInfo from, out SignCacheInfo to))
            {
                await sender.SendOnly(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)SForwardMessengerIds.StopClient,
                    Payload = serializer.Serialize(info.Id)
                }).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 测试对端的穿透记录
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)SForwardMessengerIds.TestClientForward)]
        public async Task TestClientForward(IConnection connection)
        {
            string machineid = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, machineid, out SignCacheInfo from, out SignCacheInfo to))
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;
                await sender.SendOnly(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)SForwardMessengerIds.TestClient
                }).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// 来自节点的连接
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)SForwardMessengerIds.ProxyNode)]
        public async Task ProxyNode(IConnection connection)
        {
            SForwardProxyInfo info = serializer.Deserialize<SForwardProxyInfo>(connection.ReceiveRequestWrap.Payload.Span);

            if (sForwardServerMasterTransfer.GetNode(info.NodeId, out var node) && string.IsNullOrWhiteSpace(info.MachineId) == false && signCaching.TryGet(info.MachineId, out SignCacheInfo sign) && sign.Connected)
            {
                info.Addr = node.Address;
                await sender.SendOnly(new MessageRequestWrap
                {
                    Connection = sign.Connection,
                    MessengerId = (ushort)SForwardMessengerIds.Proxy,
                    Payload = serializer.Serialize(info)
                }).ConfigureAwait(false);
            }
        }
        /// <summary>
        /// 服务器收到http连接
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        private async Task<string> WebConnect(string host, int port, ulong id)
        {
            if (sForwardServerCahing.TryGet(host, out string machineId) == false)
            {
                if (string.IsNullOrWhiteSpace(sForwardServerNodeTransfer.Node.Domain))
                {
                    return "Node domain not found";
                }
                host = host.Substring(0, host.Length - sForwardServerNodeTransfer.Node.Domain.Length - 1);
                if (sForwardServerCahing.TryGet(host, out machineId) == false)
                {
                    return "Host to machine not found";
                }
            }


            bool result = await sForwardServerNodeTransfer.ProxyNode(new SForwardProxyInfo
            {
                Domain = host,
                RemotePort = port,
                Id = id,
                BufferSize = 3,
                NodeId = sForwardServerNodeTransfer.Node.Id,
                ProtocolType = ProtocolType.Tcp,
                MachineId = machineId,
            });
            return result ? string.Empty : "Proxy node fail";
        }
        /// <summary>
        /// 服务器收到tcp连接
        /// </summary>
        /// <param name="port"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        private async Task<string> TunnelConnect(int port, ulong id)
        {
            sForwardServerCahing.TryGet(port, out string machineId);
            bool result = await sForwardServerNodeTransfer.ProxyNode(new SForwardProxyInfo
            {
                RemotePort = port,
                Id = id,
                BufferSize = 3,
                NodeId = sForwardServerNodeTransfer.Node.Id,
                ProtocolType = ProtocolType.Tcp,
                MachineId = machineId,
            });
            return result ? string.Empty : "Proxy node fail";
        }
        /// <summary>
        /// 服务器收到udp数据
        /// </summary>
        /// <param name="port"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        private async Task<bool> UdpConnect(int port, ulong id)
        {
            sForwardServerCahing.TryGet(port, out string machineId);
            return await sForwardServerNodeTransfer.ProxyNode(new SForwardProxyInfo
            {
                RemotePort = port,
                Id = id,
                BufferSize = 3,
                NodeId = sForwardServerNodeTransfer.Node.Id,
                ProtocolType = ProtocolType.Udp,
                MachineId = machineId
            });
        }


        private void ClearTask()
        {
            TimerHelper.SetIntervalLong(async () =>
            {
                var ids = sForwardServerCahing.GetMachineIds();
                if (ids.Count > 0)
                {
                    var offIds = await sForwardServerNodeTransfer.Heart(ids);

                    if (sForwardServerCahing.TryGet(offIds, out List<string> domains, out List<int> ports))
                    {
                        if (domains.Count != 0)
                        {
                            foreach (var domain in domains)
                            {
                                sForwardServerCahing.TryRemove(domain, out _);
                                proxy.RemoveHttp(domain);
                            }
                        }
                        if (ports.Count != 0)
                        {
                            foreach (var port in ports)
                            {
                                sForwardServerCahing.TryRemove(port, out _);
                                proxy.Stop(port);
                            }
                        }
                    }
                }

            }, 15000);
        }
    }

    /// <summary>
    /// 服务器穿透客户端
    /// </summary>
    public sealed class SForwardClientMessenger : IMessenger
    {
        private readonly SForwardProxy proxy;
        private readonly SForwardClientTransfer sForwardTransfer;
        private readonly ISForwardClientStore sForwardClientStore;
        private readonly ISerializer serializer;
        public SForwardClientMessenger(SForwardProxy proxy, SForwardClientTransfer sForwardTransfer, ISForwardClientStore sForwardClientStore, ISerializer serializer)
        {
            this.proxy = proxy;
            this.sForwardTransfer = sForwardTransfer;
            this.sForwardClientStore = sForwardClientStore;
            this.serializer = serializer;
        }

        /// <summary>
        /// 别人来获取穿透记录
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)SForwardMessengerIds.Get)]
        public void Get(IConnection connection)
        {
            List<SForwardInfo191> result = sForwardClientStore.Get().ToList();
            connection.Write(serializer.Serialize(result));
        }
        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)SForwardMessengerIds.AddClient)]
        public void AddClient(IConnection connection)
        {
            SForwardInfo191 sForwardInfo = serializer.Deserialize<SForwardInfo191>(connection.ReceiveRequestWrap.Payload.Span);
            sForwardTransfer.Add(sForwardInfo);
            connection.Write(Helper.TrueArray);
        }
        [MessengerId((ushort)SForwardMessengerIds.AddClient191)]
        public void AddClient191(IConnection connection)
        {
            SForwardInfo191 sForwardInfo = serializer.Deserialize<SForwardInfo191>(connection.ReceiveRequestWrap.Payload.Span);
            sForwardTransfer.Add(sForwardInfo);
            connection.Write(Helper.TrueArray);
        }
        // <summary>
        /// 删除
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)SForwardMessengerIds.RemoveClient)]
        public void RemoveClient(IConnection connection)
        {
            int id = serializer.Deserialize<int>(connection.ReceiveRequestWrap.Payload.Span);
            sForwardTransfer.Remove(id);
            connection.Write(Helper.TrueArray);
        }
        [MessengerId((ushort)SForwardMessengerIds.StartClient)]
        public void StartClient(IConnection connection)
        {
            int id = serializer.Deserialize<int>(connection.ReceiveRequestWrap.Payload.Span);
            sForwardTransfer.Start(id);
        }
        [MessengerId((ushort)SForwardMessengerIds.StopClient)]
        public void StopClient(IConnection connection)
        {
            int id = serializer.Deserialize<int>(connection.ReceiveRequestWrap.Payload.Span);
            sForwardTransfer.Stop(id);
        }

        // <summary>
        /// 测试
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)SForwardMessengerIds.TestClient)]
        public void TestClient(IConnection connection)
        {
            sForwardTransfer.SubscribeTest();
        }


        /// <summary>
        /// 收到服务器发来的连接
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)SForwardMessengerIds.Proxy)]
        public void Proxy(IConnection connection)
        {
            SForwardProxyInfo info = serializer.Deserialize<SForwardProxyInfo>(connection.ReceiveRequestWrap.Payload.Span);
            IPEndPoint server = new IPEndPoint(IPAddress.Any.Equals(info.Addr) ? connection.Address.Address : info.Addr, info.RemotePort);

            //是http
            if (string.IsNullOrWhiteSpace(info.Domain) == false)
            {
                SForwardInfo sForwardInfo = sForwardClientStore.Get(info.Domain);
                if (sForwardInfo != null)
                {
                    _ = proxy.OnConnectTcp(info.Domain, info.BufferSize, info.Id, server, sForwardInfo.LocalEP);
                }
            }
            //是端口
            else if (info.RemotePort > 0)
            {
                IPEndPoint localEP = GetLocalEP(info);
                if (localEP != null)
                {
                    if (info.ProtocolType == ProtocolType.Tcp)
                    {
                        _ = proxy.OnConnectTcp(info.RemotePort.ToString(), info.BufferSize, info.Id, server, localEP);
                    }
                    else
                    {
                        _ = proxy.OnConnectUdp(info.BufferSize, info.Id, server, localEP);
                    }
                }
            }
        }

        /// <summary>
        /// 获取这个连接请求对应的本机服务
        /// </summary>
        /// <param name="sForwardProxyInfo"></param>
        /// <returns></returns>
        private IPEndPoint GetLocalEP(SForwardProxyInfo sForwardProxyInfo)
        {
            SForwardInfo sForwardInfo = sForwardClientStore.Get().FirstOrDefault(c => c.RemotePort == sForwardProxyInfo.RemotePort || (c.RemotePortMin <= sForwardProxyInfo.RemotePort && c.RemotePortMax >= sForwardProxyInfo.RemotePort));
            if (sForwardInfo != null)
            {
                IPEndPoint localEP = IPEndPoint.Parse(sForwardInfo.LocalEP.ToString());
                if (sForwardInfo.RemotePortMin != 0 && sForwardInfo.RemotePortMax != 0)
                {
                    uint plus = (uint)(sForwardProxyInfo.RemotePort - sForwardInfo.RemotePortMin);
                    uint newIP = NetworkHelper.ToValue(localEP.Address) + plus;
                    localEP.Address = NetworkHelper.ToIP(newIP);
                }
                return localEP;
            }
            return null;
        }

    }
}
