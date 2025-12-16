using linker.libs;
using linker.libs.timer;
using linker.messenger.node;
using linker.messenger.sforward.client;
using linker.messenger.sforward.server;
using linker.messenger.sforward.server.validator;
using linker.messenger.signin;
using linker.plugins.sforward.proxy;
using System.Net;
using System.Net.Sockets;

namespace linker.messenger.sforward.messenger
{
    /// <summary>
    /// 穿透服务端
    /// </summary>
    public sealed class SForwardServerMessenger : IMessenger
    {

        private readonly SForwardProxy proxy;
        private readonly ISForwardServerCahing sforwardServerCahing;
        private readonly IMessengerSender sender;
        private readonly SignInServerCaching signCaching;
        private readonly SForwardValidatorTransfer validator;
        private readonly ISerializer serializer;
        private readonly SForwardServerMasterTransfer sforwardServerMasterTransfer;
        private readonly SForwardServerNodeTransfer sforwardServerNodeTransfer;
        private readonly SForwardServerNodeReportTransfer sforwardServerNodeReportTransfer;

        public SForwardServerMessenger(SForwardProxy proxy, ISForwardServerCahing sforwardServerCahing, IMessengerSender sender,
            SignInServerCaching signCaching, SForwardValidatorTransfer validator, ISerializer serializer,
            SForwardServerMasterTransfer sforwardServerMasterTransfer, SForwardServerNodeTransfer sforwardServerNodeTransfer,
            SForwardServerNodeReportTransfer sforwardServerNodeReportTransfer)
        {
            this.proxy = proxy;
            proxy.WebConnect = WebConnect;
            proxy.TunnelConnect = TunnelConnect;
            proxy.UdpConnect = UdpConnect;
            this.sforwardServerCahing = sforwardServerCahing;
            this.sender = sender;
            this.signCaching = signCaching;
            this.validator = validator;
            this.serializer = serializer;
            this.sforwardServerMasterTransfer = sforwardServerMasterTransfer;
            this.sforwardServerNodeTransfer = sforwardServerNodeTransfer;
            this.sforwardServerNodeReportTransfer = sforwardServerNodeReportTransfer;

            ClearTask();

        }
        /// <summary>
        /// 信标服务器
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)SForwardMessengerIds.Nodes)]
        public async Task Nodes(IConnection connection)
        {
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) == false)
            {
                connection.Write(serializer.Serialize(new List<SForwardServerNodeStoreInfo>()));
                return;
            }

            var nodes = await sforwardServerNodeReportTransfer.GetNodes(cache.Super, cache.UserId, cache.MachineId);

            connection.Write(serializer.Serialize(nodes));
        }
        /// <summary>
        /// 信标服务器
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)SForwardMessengerIds.NodeReport)]
        public async Task NodeReport(IConnection connection)
        {
            try
            {
                SForwardServerNodeReportInfoOld info = serializer.Deserialize<SForwardServerNodeReportInfoOld>(connection.ReceiveRequestWrap.Payload.Span);
                if (info.Address.Equals(IPAddress.Any) || info.Address.Equals(IPAddress.Loopback))
                {
                    info.Address = connection.Address.Address;
                }

                await sforwardServerNodeReportTransfer.Report(info.Id, info.Name, info.Address.ToString()).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
            }
            connection.Write(serializer.Serialize(VersionHelper.Version));
        }
        /// <summary>
        /// 信标服务器
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)SForwardMessengerIds.Heart)]
        public void Heart(IConnection connection)
        {
            List<string> ids = serializer.Deserialize<List<string>>(connection.ReceiveRequestWrap.Payload.Span);
            connection.Write(serializer.Serialize(ids.Except(signCaching.GetOnline()).ToList()));
        }

        /// <summary>
        /// 节点服务器
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)SForwardMessengerIds.Start)]
        public void Start(IConnection connection)
        {
            SForwardAddInfo sForwardAddInfo = serializer.Deserialize<SForwardAddInfo>(connection.ReceiveRequestWrap.Payload.Span);
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
        /// <summary>
        /// 信标服务器
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)SForwardMessengerIds.StartForward)]
        public async Task StartForward(IConnection connection)
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
                string error = await validator.Validate(cache, sForwardAddInfo).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(error) == false)
                {
                    result.Success = false;
                    result.Message = error;
                    return;
                }

                sForwardAddInfo.GroupId = cache.GroupId;
                sForwardAddInfo.MachineId = cache.MachineId;
                result = await sforwardServerMasterTransfer.Start(sForwardAddInfo, cache);
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
        /// <summary>
        /// 节点服务器
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)SForwardMessengerIds.Stop)]
        public void Stop(IConnection connection)
        {
            SForwardAddInfo sForwardAddInfo = serializer.Deserialize<SForwardAddInfo>(connection.ReceiveRequestWrap.Payload.Span);
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
        /// <summary>
        /// 信标服务器
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)SForwardMessengerIds.StopForward)]
        public async Task StopForward(IConnection connection)
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
                sForwardAddInfo.GroupId = cache.GroupId;
                sForwardAddInfo.MachineId = cache.MachineId;
                result = await sforwardServerMasterTransfer.Stop(sForwardAddInfo);
            }
            catch (Exception)
            {
            }
            finally
            {
                connection.Write(serializer.Serialize(result));
            }
        }


        private void Remove(SForwardAddInfo sForwardAddInfo, string machineId, SForwardAddResultInfo result)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sForwardAddInfo.Domain) == false)
                {
                    if (PortRange(sForwardAddInfo.Domain, out int min, out int max))
                    {
                        for (int port = min; port <= max; port++)
                        {
                            if (sforwardServerCahing.TryRemove(port, machineId, sForwardAddInfo.NodeId, out _))
                            {
                                proxy.Stop(port);
                            }
                        }
                    }
                    else
                    {
                        sforwardServerCahing.TryRemove(sForwardAddInfo.Domain, machineId, sForwardAddInfo.NodeId, out _);
                        proxy.RemoveHttp(sForwardAddInfo.Domain);
                        result.Message = $"domain 【{sForwardAddInfo.Domain}】 remove success";
                    }
                    return;
                }

                if (sForwardAddInfo.RemotePort > 0)
                {
                    sforwardServerCahing.TryRemove(sForwardAddInfo.RemotePort, machineId, sForwardAddInfo.NodeId, out _);
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
        private void Remove(SForwardAddInfo sForwardAddInfo, SForwardAddResultInfo result)
        {
            Remove(sForwardAddInfo, sForwardAddInfo.MachineId, result);
        }
        private void Add(SForwardAddInfo sForwardAddInfo, string machineId, string groupid, SForwardAddResultInfo result, bool super, double bandwidth)
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
                            if (sforwardServerCahing.TryAdd(port, machineId, sForwardAddInfo.NodeId))
                            {
                                proxy.Stop(port);
                                result.Message = proxy.Start(port, 3, groupid, super, bandwidth);
                                if (string.IsNullOrWhiteSpace(result.Message) == false)
                                {
                                    LoggerHelper.Instance.Error(result.Message);
                                    sforwardServerCahing.TryRemove(port, machineId, out _);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (sforwardServerCahing.TryAdd(sForwardAddInfo.Domain, machineId, sForwardAddInfo.NodeId) == false)
                        {
                            result.Success = false;
                            result.Message = $"domain 【{sForwardAddInfo.Domain}】 already exists";
                            LoggerHelper.Instance.Error(result.Message);
                        }
                        else
                        {
                            proxy.RemoveHttp(sForwardAddInfo.Domain);
                            proxy.AddHttp(sForwardAddInfo.Domain, super, bandwidth);
                            result.Message = $"domain 【{sForwardAddInfo.Domain}】 add success";
                        }
                    }
                    return;
                }
                //如果是端口
                if (sForwardAddInfo.RemotePort > 0)
                {
                    if (sforwardServerCahing.TryAdd(sForwardAddInfo.RemotePort, machineId, sForwardAddInfo.NodeId) == false)
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
                            sforwardServerCahing.TryRemove(sForwardAddInfo.RemotePort, machineId, out _);
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
        private void Add(SForwardAddInfo sForwardAddInfo, SForwardAddResultInfo result)
        {
            Add(sForwardAddInfo, sForwardAddInfo.MachineId, sForwardAddInfo.GroupId, result, sForwardAddInfo.Super, sForwardAddInfo.Bandwidth);
        }
        private static bool PortRange(string str, out int min, out int max)
        {
            min = 0; max = 0;
            string[] arr = str.Split('/');
            return arr.Length == 2 && int.TryParse(arr[0], out min) && int.TryParse(arr[1], out max);
        }


        /// <summary>
        /// 信标服务器
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
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
        /// 信标服务器
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
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
        /// <summary>
        /// 信标服务器
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
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
        /// <summary>
        /// 信标服务器
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
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
        /// <summary>
        /// 信标服务器
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
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
        /// 信标服务器
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
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
        /// 信标服务器
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)SForwardMessengerIds.SignIn)]
        public async Task SignIn(IConnection connection)
        {
            ValueTuple<string, string, string> kv = serializer.Deserialize<ValueTuple<string, string, string>>(connection.ReceiveRequestWrap.Payload.Span);
            if (await sforwardServerNodeReportTransfer.SignIn(kv.Item1, kv.Item2, kv.Item3, connection).ConfigureAwait(false))
            {
                connection.Write(Helper.TrueArray);
            }
            else
            {
                connection.Write(Helper.FalseArray);
            }
        }
        /// <summary>
        /// 信标服务器
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)SForwardMessengerIds.Report)]
        public async Task Report(IConnection connection)
        {
            SForwardServerNodeReportInfo info = serializer.Deserialize<SForwardServerNodeReportInfo>(connection.ReceiveRequestWrap.Payload.Span);
            await sforwardServerNodeReportTransfer.Report(connection, info).ConfigureAwait(false);
        }

        /// <summary>
        /// 信标服务器
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)SForwardMessengerIds.ShareForward)]
        public async Task ShareForward(IConnection connection)
        {
            string id = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo from) == false || from.Super == false)
            {
                connection.Write(serializer.Serialize("need super key"));
                return;
            }
            connection.Write(serializer.Serialize(await sforwardServerNodeReportTransfer.GetShareKeyForward(id)));
        }
        /// <summary>
        /// 节点服务器
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)SForwardMessengerIds.Share)]
        public async Task Share(IConnection connection)
        {
            connection.Write(serializer.Serialize(await sforwardServerNodeReportTransfer.GetShareKey(connection)));
        }

        /// <summary>
        /// 信标服务器
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)SForwardMessengerIds.Import)]
        public async Task Import(IConnection connection)
        {
            string sharekey = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);

            if (signCaching.TryGet(connection.Id, out SignCacheInfo from) == false || from.Super == false)
            {
                connection.Write(serializer.Serialize("need super key"));
                return;
            }

            string result = await sforwardServerNodeReportTransfer.Import(sharekey).ConfigureAwait(false);
            connection.Write(serializer.Serialize(result));
        }
        /// <summary>
        /// 信标服务器
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)SForwardMessengerIds.Remove)]
        public async Task Remove(IConnection connection)
        {
            string id = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo from) == false || from.Super == false)
            {
                connection.Write(serializer.Serialize("need super key"));
                return;
            }

            bool result = await sforwardServerNodeReportTransfer.Remove(id).ConfigureAwait(false);
            connection.Write(serializer.Serialize(result ? string.Empty : "remove fail"));
        }
        /// <summary>
        /// 信标服务器
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)SForwardMessengerIds.UpdateForward)]
        public async Task UpdateForward(IConnection connection)
        {
            SForwardServerNodeStoreInfo info = serializer.Deserialize<SForwardServerNodeStoreInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo from) == false || from.Super == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }

            bool result = await sforwardServerNodeReportTransfer.UpdateForward(info).ConfigureAwait(false);
            connection.Write(result ? Helper.TrueArray : Helper.FalseArray);
        }
        /// <summary>
        /// 节点服务器
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)SForwardMessengerIds.Update)]
        public async Task Update(IConnection connection)
        {
            SForwardServerNodeStoreInfo info = serializer.Deserialize<SForwardServerNodeStoreInfo>(connection.ReceiveRequestWrap.Payload.Span);
            await sforwardServerNodeReportTransfer.Update(connection, info).ConfigureAwait(false);
        }
        /// <summary>
        /// 信标服务器
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>

        [MessengerId((ushort)SForwardMessengerIds.UpgradeForward)]
        public async Task UpgradeForward(IConnection connection)
        {
            KeyValuePair<string, string> info = serializer.Deserialize<KeyValuePair<string, string>>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo from) == false || from.Super == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }

            bool result = await sforwardServerNodeReportTransfer.UpgradeForward(info.Key, info.Value).ConfigureAwait(false);
            connection.Write(result ? Helper.TrueArray : Helper.FalseArray);
        }
        /// <summary>
        /// 节点服务器
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)SForwardMessengerIds.Upgrade)]
        public async Task Upgrade(IConnection connection)
        {
            string version = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            await sforwardServerNodeReportTransfer.Upgrade(connection, version).ConfigureAwait(false);
        }
        /// <summary>
        /// 信标服务器
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>

        [MessengerId((ushort)SForwardMessengerIds.ExitForward)]
        public async Task ExitForward(IConnection connection)
        {
            string nodeid = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo from) == false || from.Super == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }

            bool result = await sforwardServerNodeReportTransfer.ExitForward(nodeid).ConfigureAwait(false);
            connection.Write(result ? Helper.TrueArray : Helper.FalseArray);
        }
        /// <summary>
        /// 节点服务器
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)SForwardMessengerIds.Exit)]
        public async Task Exit(IConnection connection)
        {
            await sforwardServerNodeReportTransfer.Exit(connection).ConfigureAwait(false);
        }

        /// <summary>
        /// 信标服务器
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)SForwardMessengerIds.MastersForward)]
        public async Task MastersForward(IConnection connection)
        {
            MastersRequestInfo info = serializer.Deserialize<MastersRequestInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo from) == false || from.Super == false)
            {
                connection.Write(serializer.Serialize(new MastersResponseInfo()));
                return;
            }

            MastersResponseInfo resp = await sforwardServerNodeReportTransfer.MastersForward(info).ConfigureAwait(false);
            connection.Write(serializer.Serialize(resp));
        }
        /// <summary>
        /// 节点服务器
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)SForwardMessengerIds.Masters)]
        public async Task Masters(IConnection connection)
        {
            MastersRequestInfo info = serializer.Deserialize<MastersRequestInfo>(connection.ReceiveRequestWrap.Payload.Span);
            MastersResponseInfo resp = await sforwardServerNodeReportTransfer.Masters(connection, info).ConfigureAwait(false);
            connection.Write(serializer.Serialize(resp));
        }

        /// <summary>
        /// 信标服务器
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)SForwardMessengerIds.DenysForward)]
        public async Task DenysForward(IConnection connection)
        {
            MasterDenyStoreRequestInfo info = serializer.Deserialize<MasterDenyStoreRequestInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo from) == false || from.Super == false)
            {
                connection.Write(serializer.Serialize(new MasterDenyStoreResponseInfo()));
                return;
            }

            MasterDenyStoreResponseInfo resp = await sforwardServerNodeReportTransfer.DenysForward(info).ConfigureAwait(false);
            connection.Write(serializer.Serialize(resp));
        }
        /// <summary>
        /// 节点服务器
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)SForwardMessengerIds.Denys)]
        public async Task Denys(IConnection connection)
        {
            MasterDenyStoreRequestInfo info = serializer.Deserialize<MasterDenyStoreRequestInfo>(connection.ReceiveRequestWrap.Payload.Span);
            MasterDenyStoreResponseInfo resp = await sforwardServerNodeReportTransfer.Denys(connection, info).ConfigureAwait(false);
            connection.Write(serializer.Serialize(resp));
        }
        public async Task DenysAddForward(IConnection connection)
        {
            MasterDenyAddInfo info = serializer.Deserialize<MasterDenyAddInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo from) == false || from.Super == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }

            bool resp = await sforwardServerNodeReportTransfer.DenysAddForward(info).ConfigureAwait(false);
            connection.Write(resp ? Helper.TrueArray : Helper.FalseArray);
        }
        [MessengerId((ushort)SForwardMessengerIds.DenysAdd)]
        public async Task DenysAdd(IConnection connection)
        {
            MasterDenyAddInfo info = serializer.Deserialize<MasterDenyAddInfo>(connection.ReceiveRequestWrap.Payload.Span);
            bool resp = await sforwardServerNodeReportTransfer.DenysAdd(connection, info).ConfigureAwait(false);
            connection.Write(resp ? Helper.TrueArray : Helper.FalseArray);
        }
        [MessengerId((ushort)SForwardMessengerIds.DenysDelForward)]
        public async Task DenysDelForward(IConnection connection)
        {
            MasterDenyDelInfo info = serializer.Deserialize<MasterDenyDelInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo from) == false || from.Super == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }

            bool resp = await sforwardServerNodeReportTransfer.DenysDelForward(info).ConfigureAwait(false);
            connection.Write(resp ? Helper.TrueArray : Helper.FalseArray);
        }
        [MessengerId((ushort)SForwardMessengerIds.DenysDel)]
        public async Task DenysDel(IConnection connection)
        {
            MasterDenyDelInfo info = serializer.Deserialize<MasterDenyDelInfo>(connection.ReceiveRequestWrap.Payload.Span);
            bool resp = await sforwardServerNodeReportTransfer.DenysDel(connection, info).ConfigureAwait(false);
            connection.Write(resp ? Helper.TrueArray : Helper.FalseArray);
        }

        /// <summary>
        /// 信标服务器收到来自节点的连接
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)SForwardMessengerIds.ProxyForward)]
        public async Task ProxyForward(IConnection connection)
        {
            SForwardProxyInfo info = serializer.Deserialize<SForwardProxyInfo>(connection.ReceiveRequestWrap.Payload.Span);
            var node = await sforwardServerNodeReportTransfer.GetNode(info.NodeId).ConfigureAwait(false);
            if (node == null || string.IsNullOrWhiteSpace(info.MachineId))
            {
                return;
            }
            if (signCaching.TryGet(info.MachineId, out SignCacheInfo sign) == false || sign.Connected == false)
            {
                return;
            }

            info.Addr = connection.Address.Address;
            var resp = await sender.SendReply(new MessageRequestWrap
            {
                Connection = sign.Connection,
                MessengerId = (ushort)SForwardMessengerIds.Proxy,
                Payload = serializer.Serialize(info),
                Timeout = 5000,
            }).ConfigureAwait(false);
            /*
            if (resp.Code != MessageResponeCodes.OK || resp.Data.Span.SequenceEqual(Helper.TrueArray) == false)
            {

            }
            */
        }
        /// <summary>
        /// 节点服务器收到http连接
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        private async Task<string> WebConnect(string host, int port, ulong id)
        {
            if (sforwardServerCahing.TryGet(host, out string machineId, out string nodeid) == false)
            {
                if (string.IsNullOrWhiteSpace(sforwardServerNodeReportTransfer.Config.Domain))
                {
                    return "Node domain not found";
                }
                host = host.Substring(0, host.Length - sforwardServerNodeReportTransfer.Config.Domain.Length - 1);
                if (sforwardServerCahing.TryGet(host, out machineId, out nodeid) == false)
                {
                    return "Host to machine not found";
                }
            }

            bool result = await sforwardServerNodeTransfer.ProxyForward(new SForwardProxyInfo
            {
                Domain = host,
                RemotePort = port,
                Id = id,
                BufferSize = 3,
                NodeId = nodeid,
                ProtocolType = ProtocolType.Tcp,
                MachineId = machineId,
            });
            return result ? string.Empty : "Proxy node fail";
        }
        /// <summary>
        /// 节点服务器收到tcp连接
        /// </summary>
        /// <param name="port"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        private async Task<string> TunnelConnect(int port, ulong id)
        {
            sforwardServerCahing.TryGet(port, out string machineId, out string nodeid);
            if (string.IsNullOrWhiteSpace(machineId) || string.IsNullOrWhiteSpace(nodeid))
            {

            }

            bool result = await sforwardServerNodeTransfer.ProxyForward(new SForwardProxyInfo
            {
                RemotePort = port,
                Id = id,
                BufferSize = 3,
                NodeId = nodeid,
                ProtocolType = ProtocolType.Tcp,
                MachineId = machineId,
            });
            return result ? string.Empty : "Proxy node fail";
        }
        /// <summary>
        /// 节点服务器收到udp数据
        /// </summary>
        /// <param name="port"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        private async Task<bool> UdpConnect(int port, ulong id)
        {
            sforwardServerCahing.TryGet(port, out string machineId, out string nodeid);
            if (string.IsNullOrWhiteSpace(machineId) || string.IsNullOrWhiteSpace(nodeid))
            {

            }
            return await sforwardServerNodeTransfer.ProxyForward(new SForwardProxyInfo
            {
                RemotePort = port,
                Id = id,
                BufferSize = 3,
                NodeId = nodeid,
                ProtocolType = ProtocolType.Udp,
                MachineId = machineId
            });
        }


        private void ClearTask()
        {
            TimerHelper.SetIntervalLong(async () =>
            {
                var dic = sforwardServerCahing.GetMachineIds();
                if (dic.Count == 0) return;

                foreach (var kv in dic)
                {
                    if (kv.Value.Count == 0) continue;

                    var allIds = kv.Value.Select(c => c.MachineId).ToList();
                    var offIds = await sforwardServerNodeTransfer.Heart(allIds, kv.Key);
                    var onlineIds = allIds.Except(offIds);
                    foreach (var info in kv.Value.Where(c => onlineIds.Contains(c.MachineId)))
                    {
                        info.LastTime = Environment.TickCount64;
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
            List<SForwardInfo> result = sForwardClientStore.Get().ToList();
            connection.Write(serializer.Serialize(result));
        }
        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="connection"></param>

        [MessengerId((ushort)SForwardMessengerIds.AddClient)]
        public void AddClient(IConnection connection)
        {
            SForwardInfo sForwardInfo = serializer.Deserialize<SForwardInfo>(connection.ReceiveRequestWrap.Payload.Span);
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
            IPEndPoint server = new IPEndPoint(IPAddress.Any.Equals(info.Addr) || IPAddress.Loopback.Equals(info.Addr) ? connection.Address.Address : info.Addr, info.RemotePort);
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
            connection.Write(Helper.TrueArray);
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
