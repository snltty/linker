using linker.libs;
using linker.libs.timer;
using linker.messenger.node;
using linker.messenger.reverse.client;
using linker.messenger.reverse.proxy;
using linker.messenger.reverse.server;
using linker.messenger.reverse.server.validator;
using linker.messenger.signin;
using System.Net;
using System.Net.Sockets;

namespace linker.messenger.reverse.messenger
{
    /// <summary>
    /// 穿透服务端
    /// </summary>
    public sealed class ReverseServerMessenger : IMessenger
    {

        private readonly ReverseProxy proxy;
        private readonly IReverseServerCahing ReverseServerCahing;
        private readonly IMessengerSender sender;
        private readonly SignInServerCaching signCaching;
        private readonly ReverseValidatorTransfer validator;
        private readonly ISerializer serializer;
        private readonly ReverseServerMasterTransfer ReverseServerMasterTransfer;
        private readonly ReverseServerNodeTransfer ReverseServerNodeTransfer;
        private readonly ReverseServerNodeReportTransfer ReverseServerNodeReportTransfer;

        public ReverseServerMessenger(ReverseProxy proxy, IReverseServerCahing ReverseServerCahing, IMessengerSender sender,
            SignInServerCaching signCaching, ReverseValidatorTransfer validator, ISerializer serializer,
            ReverseServerMasterTransfer ReverseServerMasterTransfer, ReverseServerNodeTransfer ReverseServerNodeTransfer,
            ReverseServerNodeReportTransfer ReverseServerNodeReportTransfer)
        {
            this.proxy = proxy;
            proxy.WebConnect = WebConnect;
            proxy.TunnelConnect = TunnelConnect;
            proxy.UdpConnect = UdpConnect;
            this.ReverseServerCahing = ReverseServerCahing;
            this.sender = sender;
            this.signCaching = signCaching;
            this.validator = validator;
            this.serializer = serializer;
            this.ReverseServerMasterTransfer = ReverseServerMasterTransfer;
            this.ReverseServerNodeTransfer = ReverseServerNodeTransfer;
            this.ReverseServerNodeReportTransfer = ReverseServerNodeReportTransfer;

            ClearTask();

        }
        /// <summary>
        /// 信标服务器
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)ReverseMessengerIds.Nodes)]
        public async Task Nodes(IConnection connection)
        {
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) == false)
            {
                connection.Write(serializer.Serialize(new List<ReverseServerNodeStoreInfo>()));
                return;
            }

            var nodes = await ReverseServerNodeReportTransfer.GetNodes(cache.Super, cache.UserId, cache.MachineId);

            connection.Write(serializer.Serialize(nodes));
        }
        /// <summary>
        /// 信标服务器
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)ReverseMessengerIds.NodeReport)]
        public async Task NodeReport(IConnection connection)
        {
            try
            {
                ReverseServerNodeReportInfoOld info = serializer.Deserialize<ReverseServerNodeReportInfoOld>(connection.ReceiveRequestWrap.Payload.Span);
                if (info.Address.Equals(IPAddress.Any) || info.Address.Equals(IPAddress.Loopback))
                {
                    info.Address = connection.Address.Address;
                }

                await ReverseServerNodeReportTransfer.Report(info.Id, info.Name, info.Address.ToString()).ConfigureAwait(false);
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
        [MessengerId((ushort)ReverseMessengerIds.Heart)]
        public void Heart(IConnection connection)
        {
            List<string> ids = serializer.Deserialize<List<string>>(connection.ReceiveRequestWrap.Payload.Span);
            connection.Write(serializer.Serialize(ids.Except(signCaching.GetOnline()).ToList()));
        }

        /// <summary>
        /// 节点服务器
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)ReverseMessengerIds.Start)]
        public void Start(IConnection connection)
        {
            ReverseAddInfo ReverseAddInfo = serializer.Deserialize<ReverseAddInfo>(connection.ReceiveRequestWrap.Payload.Span);
            ReverseAddResultInfo result = new ReverseAddResultInfo { Success = true, BufferSize = 3 };
            try
            {
                Add(ReverseAddInfo, result);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Reverse fail : {ex.Message}";
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
        [MessengerId((ushort)ReverseMessengerIds.StartForward)]
        public async Task StartForward(IConnection connection)
        {
            ReverseAddInfo ReverseAddInfo = serializer.Deserialize<ReverseAddInfo>(connection.ReceiveRequestWrap.Payload.Span);
            ReverseAddResultInfo result = new ReverseAddResultInfo { Success = true, BufferSize = 3 };
            try
            {
                if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) == false)
                {
                    result.Success = false;
                    result.Message = "need sign in";
                    return;
                }
                string error = await validator.Validate(cache, ReverseAddInfo).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(error) == false)
                {
                    result.Success = false;
                    result.Message = error;
                    return;
                }

                ReverseAddInfo.GroupId = cache.GroupId;
                ReverseAddInfo.MachineId = cache.MachineId;
                result = await ReverseServerMasterTransfer.Start(ReverseAddInfo, cache);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Reverse fail : {ex.Message}";
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
        [MessengerId((ushort)ReverseMessengerIds.Stop)]
        public void Stop(IConnection connection)
        {
            ReverseAddInfo ReverseAddInfo = serializer.Deserialize<ReverseAddInfo>(connection.ReceiveRequestWrap.Payload.Span);
            ReverseAddResultInfo result = new ReverseAddResultInfo { Success = true };

            try
            {
                Remove(ReverseAddInfo, result);
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
        [MessengerId((ushort)ReverseMessengerIds.StopForward)]
        public async Task StopForward(IConnection connection)
        {
            ReverseAddInfo ReverseAddInfo = serializer.Deserialize<ReverseAddInfo>(connection.ReceiveRequestWrap.Payload.Span);
            ReverseAddResultInfo result = new ReverseAddResultInfo { Success = true };

            try
            {
                if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) == false)
                {
                    result.Success = false;
                    result.Message = "need sign in";
                    return;
                }
                ReverseAddInfo.GroupId = cache.GroupId;
                ReverseAddInfo.MachineId = cache.MachineId;
                result = await ReverseServerMasterTransfer.Stop(ReverseAddInfo);
            }
            catch (Exception)
            {
            }
            finally
            {
                connection.Write(serializer.Serialize(result));
            }
        }


        private void Remove(ReverseAddInfo ReverseAddInfo, string machineId, ReverseAddResultInfo result)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ReverseAddInfo.Domain) == false)
                {
                    if (PortRange(ReverseAddInfo.Domain, out int min, out int max))
                    {
                        for (int port = min; port <= max; port++)
                        {
                            if (ReverseServerCahing.TryRemove(port, machineId, ReverseAddInfo.NodeId, out _))
                            {
                                proxy.Stop(port);
                            }
                        }
                    }
                    else
                    {
                        ReverseServerCahing.TryRemove(ReverseAddInfo.Domain, machineId, ReverseAddInfo.NodeId, out _);
                        proxy.RemoveHttp(ReverseAddInfo.Domain);
                        result.Message = $"domain 【{ReverseAddInfo.Domain}】 remove success";
                    }
                    return;
                }

                if (ReverseAddInfo.RemotePort > 0)
                {
                    ReverseServerCahing.TryRemove(ReverseAddInfo.RemotePort, machineId, ReverseAddInfo.NodeId, out _);
                    proxy.Stop(ReverseAddInfo.RemotePort);
                    result.Message = $"port 【{ReverseAddInfo.RemotePort}】 remove success";
                    return;
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Reverse fail : {ex.Message}";
            }
        }
        private void Remove(ReverseAddInfo ReverseAddInfo, ReverseAddResultInfo result)
        {
            Remove(ReverseAddInfo, ReverseAddInfo.MachineId, result);
        }
        private void Add(ReverseAddInfo ReverseAddInfo, string machineId, string groupid, ReverseAddResultInfo result, bool super, double bandwidth)
        {
            try
            {
                //有域名，
                if (string.IsNullOrWhiteSpace(ReverseAddInfo.Domain) == false)
                {
                    //有可能是 端口范围，不是真的域名
                    if (PortRange(ReverseAddInfo.Domain, out int min, out int max))
                    {
                        for (int port = min; port <= max; port++)
                        {
                            if (ReverseServerCahing.TryAdd(port, machineId, ReverseAddInfo.NodeId))
                            {
                                proxy.Stop(port);
                                result.Message = proxy.Start(port, 3, groupid, super, bandwidth);
                                if (string.IsNullOrWhiteSpace(result.Message) == false)
                                {
                                    LoggerHelper.Instance.Error(result.Message);
                                    ReverseServerCahing.TryRemove(port, machineId, out _);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (ReverseServerCahing.TryAdd(ReverseAddInfo.Domain, machineId, ReverseAddInfo.NodeId) == false)
                        {
                            result.Success = false;
                            result.Message = $"domain 【{ReverseAddInfo.Domain}】 already exists";
                            LoggerHelper.Instance.Error(result.Message);
                        }
                        else
                        {
                            proxy.RemoveHttp(ReverseAddInfo.Domain);
                            proxy.AddHttp(ReverseAddInfo.Domain, super, bandwidth);
                            result.Message = $"domain 【{ReverseAddInfo.Domain}】 add success";
                        }
                    }
                    return;
                }
                //如果是端口
                if (ReverseAddInfo.RemotePort > 0)
                {
                    if (ReverseServerCahing.TryAdd(ReverseAddInfo.RemotePort, machineId, ReverseAddInfo.NodeId) == false)
                    {

                        result.Success = false;
                        result.Message = $"port 【{ReverseAddInfo.RemotePort}】 already exists";
                        LoggerHelper.Instance.Error(result.Message);
                    }
                    else
                    {
                        proxy.Stop(ReverseAddInfo.RemotePort);
                        string msg = proxy.Start(ReverseAddInfo.RemotePort, 3, groupid, super, bandwidth);
                        if (string.IsNullOrWhiteSpace(msg) == false)
                        {
                            result.Success = false;
                            result.Message = $"port 【{ReverseAddInfo.RemotePort}】 add fail : {msg}";
                            ReverseServerCahing.TryRemove(ReverseAddInfo.RemotePort, machineId, out _);
                            LoggerHelper.Instance.Error(result.Message);
                        }
                        else
                        {
                            result.Message = $"port 【{ReverseAddInfo.RemotePort}】 add success";
                        }
                    }
                    return;
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Reverse fail : {ex.Message}";
            }
        }
        private void Add(ReverseAddInfo ReverseAddInfo, ReverseAddResultInfo result)
        {
            Add(ReverseAddInfo, ReverseAddInfo.MachineId, ReverseAddInfo.GroupId, result, ReverseAddInfo.Super, ReverseAddInfo.Bandwidth);
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
        [MessengerId((ushort)ReverseMessengerIds.GetForward)]
        public void GetForward(IConnection connection)
        {
            string machineId = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, machineId, out SignCacheInfo from, out SignCacheInfo to))
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;
                sender.SendReply(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)ReverseMessengerIds.Get,
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
                        }, (ushort)ReverseMessengerIds.GetForward).ConfigureAwait(false);
                    }
                }).ConfigureAwait(false);
            }
        }
        /// <summary>
        /// 信标服务器
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)ReverseMessengerIds.AddClientForward)]
        public async Task AddClientForward(IConnection connection)
        {
            ReverseAddForwardInfo info = serializer.Deserialize<ReverseAddForwardInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, info.MachineId, out SignCacheInfo from, out SignCacheInfo to))
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;
                await sender.SendReply(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)ReverseMessengerIds.AddClient,
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
                        }, (ushort)ReverseMessengerIds.AddClientForward).ConfigureAwait(false);
                    }
                }).ConfigureAwait(false);
            }
        }
        /// <summary>
        /// 信标服务器
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)ReverseMessengerIds.RemoveClientForward)]
        public async Task RemoveClientForward(IConnection connection)
        {
            ReverseRemoveForwardInfo info = serializer.Deserialize<ReverseRemoveForwardInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, info.MachineId, out SignCacheInfo from, out SignCacheInfo to))
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;
                await sender.SendReply(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)ReverseMessengerIds.RemoveClient,
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
                        }, (ushort)ReverseMessengerIds.RemoveClientForward).ConfigureAwait(false);
                    }
                }).ConfigureAwait(false);
            }
        }
        /// <summary>
        /// 信标服务器
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)ReverseMessengerIds.StartClientForward)]
        public async Task StartClientForward(IConnection connection)
        {
            ReverseRemoveForwardInfo info = serializer.Deserialize<ReverseRemoveForwardInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, info.MachineId, out SignCacheInfo from, out SignCacheInfo to))
            {
                await sender.SendOnly(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)ReverseMessengerIds.StartClient,
                    Payload = serializer.Serialize(info.Id)
                }).ConfigureAwait(false);
            }
        }
        /// <summary>
        /// 信标服务器
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)ReverseMessengerIds.StopClientForward)]
        public async Task StopClientForward(IConnection connection)
        {
            ReverseRemoveForwardInfo info = serializer.Deserialize<ReverseRemoveForwardInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, info.MachineId, out SignCacheInfo from, out SignCacheInfo to))
            {
                await sender.SendOnly(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)ReverseMessengerIds.StopClient,
                    Payload = serializer.Serialize(info.Id)
                }).ConfigureAwait(false);
            }
        }
        /// <summary>
        /// 信标服务器
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)ReverseMessengerIds.TestClientForward)]
        public async Task TestClientForward(IConnection connection)
        {
            string machineid = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, machineid, out SignCacheInfo from, out SignCacheInfo to))
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;
                await sender.SendOnly(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)ReverseMessengerIds.TestClient
                }).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 信标服务器
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)ReverseMessengerIds.SignIn)]
        public async Task SignIn(IConnection connection)
        {
            ValueTuple<string, string, string> kv = serializer.Deserialize<ValueTuple<string, string, string>>(connection.ReceiveRequestWrap.Payload.Span);
            if (await ReverseServerNodeReportTransfer.SignIn(kv.Item1, kv.Item2, kv.Item3, connection).ConfigureAwait(false))
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
        [MessengerId((ushort)ReverseMessengerIds.Report)]
        public async Task Report(IConnection connection)
        {
            ReverseServerNodeReportInfo info = serializer.Deserialize<ReverseServerNodeReportInfo>(connection.ReceiveRequestWrap.Payload.Span);
            await ReverseServerNodeReportTransfer.Report(connection, info).ConfigureAwait(false);
        }

        /// <summary>
        /// 信标服务器
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)ReverseMessengerIds.ShareForward)]
        public async Task ShareForward(IConnection connection)
        {
            string id = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo from) == false || from.Super == false)
            {
                connection.Write(serializer.Serialize("need super key"));
                return;
            }
            connection.Write(serializer.Serialize(await ReverseServerNodeReportTransfer.GetShareKeyForward(id)));
        }
        /// <summary>
        /// 节点服务器
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)ReverseMessengerIds.Share)]
        public async Task Share(IConnection connection)
        {
            connection.Write(serializer.Serialize(await ReverseServerNodeReportTransfer.GetShareKey(connection)));
        }

        /// <summary>
        /// 信标服务器
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)ReverseMessengerIds.Import)]
        public async Task Import(IConnection connection)
        {
            string sharekey = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);

            if (signCaching.TryGet(connection.Id, out SignCacheInfo from) == false || from.Super == false)
            {
                connection.Write(serializer.Serialize("need super key"));
                return;
            }

            string result = await ReverseServerNodeReportTransfer.Import(sharekey).ConfigureAwait(false);
            connection.Write(serializer.Serialize(result));
        }
        /// <summary>
        /// 信标服务器
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)ReverseMessengerIds.Remove)]
        public async Task Remove(IConnection connection)
        {
            string id = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo from) == false || from.Super == false)
            {
                connection.Write(serializer.Serialize("need super key"));
                return;
            }

            bool result = await ReverseServerNodeReportTransfer.Remove(id).ConfigureAwait(false);
            connection.Write(serializer.Serialize(result ? string.Empty : "remove fail"));
        }
        /// <summary>
        /// 信标服务器
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)ReverseMessengerIds.UpdateForward)]
        public async Task UpdateForward(IConnection connection)
        {
            ReverseServerNodeStoreInfo info = serializer.Deserialize<ReverseServerNodeStoreInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo from) == false || from.Super == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }

            bool result = await ReverseServerNodeReportTransfer.UpdateForward(info).ConfigureAwait(false);
            connection.Write(result ? Helper.TrueArray : Helper.FalseArray);
        }
        /// <summary>
        /// 节点服务器
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)ReverseMessengerIds.Update)]
        public async Task Update(IConnection connection)
        {
            ReverseServerNodeStoreInfo info = serializer.Deserialize<ReverseServerNodeStoreInfo>(connection.ReceiveRequestWrap.Payload.Span);
            await ReverseServerNodeReportTransfer.Update(connection, info).ConfigureAwait(false);
        }
        /// <summary>
        /// 信标服务器
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>

        [MessengerId((ushort)ReverseMessengerIds.UpgradeForward)]
        public async Task UpgradeForward(IConnection connection)
        {
            KeyValuePair<string, string> info = serializer.Deserialize<KeyValuePair<string, string>>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo from) == false || from.Super == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }

            bool result = await ReverseServerNodeReportTransfer.UpgradeForward(info.Key, info.Value).ConfigureAwait(false);
            connection.Write(result ? Helper.TrueArray : Helper.FalseArray);
        }
        /// <summary>
        /// 节点服务器
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)ReverseMessengerIds.Upgrade)]
        public async Task Upgrade(IConnection connection)
        {
            string version = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            await ReverseServerNodeReportTransfer.Upgrade(connection, version).ConfigureAwait(false);
        }
        /// <summary>
        /// 信标服务器
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>

        [MessengerId((ushort)ReverseMessengerIds.ExitForward)]
        public async Task ExitForward(IConnection connection)
        {
            string nodeid = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo from) == false || from.Super == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }

            bool result = await ReverseServerNodeReportTransfer.ExitForward(nodeid).ConfigureAwait(false);
            connection.Write(result ? Helper.TrueArray : Helper.FalseArray);
        }
        /// <summary>
        /// 节点服务器
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)ReverseMessengerIds.Exit)]
        public async Task Exit(IConnection connection)
        {
            await ReverseServerNodeReportTransfer.Exit(connection).ConfigureAwait(false);
        }

        /// <summary>
        /// 信标服务器
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)ReverseMessengerIds.MasterReverse)]
        public async Task MasterReverse(IConnection connection)
        {
            MastersRequestInfo info = serializer.Deserialize<MastersRequestInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo from) == false || from.Super == false)
            {
                connection.Write(serializer.Serialize(new MastersResponseInfo()));
                return;
            }

            MastersResponseInfo resp = await ReverseServerNodeReportTransfer.MastersForward(info).ConfigureAwait(false);
            connection.Write(serializer.Serialize(resp));
        }
        /// <summary>
        /// 节点服务器
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)ReverseMessengerIds.Masters)]
        public async Task Masters(IConnection connection)
        {
            MastersRequestInfo info = serializer.Deserialize<MastersRequestInfo>(connection.ReceiveRequestWrap.Payload.Span);
            MastersResponseInfo resp = await ReverseServerNodeReportTransfer.Masters(connection, info).ConfigureAwait(false);
            connection.Write(serializer.Serialize(resp));
        }

        /// <summary>
        /// 信标服务器
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)ReverseMessengerIds.DenyReverse)]
        public async Task DenyReverse(IConnection connection)
        {
            MasterDenyStoreRequestInfo info = serializer.Deserialize<MasterDenyStoreRequestInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo from) == false || from.Super == false)
            {
                connection.Write(serializer.Serialize(new MasterDenyStoreResponseInfo()));
                return;
            }

            MasterDenyStoreResponseInfo resp = await ReverseServerNodeReportTransfer.DenysForward(info).ConfigureAwait(false);
            connection.Write(serializer.Serialize(resp));
        }
        /// <summary>
        /// 节点服务器
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)ReverseMessengerIds.Denys)]
        public async Task Denys(IConnection connection)
        {
            MasterDenyStoreRequestInfo info = serializer.Deserialize<MasterDenyStoreRequestInfo>(connection.ReceiveRequestWrap.Payload.Span);
            MasterDenyStoreResponseInfo resp = await ReverseServerNodeReportTransfer.Denys(connection, info).ConfigureAwait(false);
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

            bool resp = await ReverseServerNodeReportTransfer.DenysAddForward(info).ConfigureAwait(false);
            connection.Write(resp ? Helper.TrueArray : Helper.FalseArray);
        }
        [MessengerId((ushort)ReverseMessengerIds.DenysAdd)]
        public async Task DenysAdd(IConnection connection)
        {
            MasterDenyAddInfo info = serializer.Deserialize<MasterDenyAddInfo>(connection.ReceiveRequestWrap.Payload.Span);
            bool resp = await ReverseServerNodeReportTransfer.DenysAdd(connection, info).ConfigureAwait(false);
            connection.Write(resp ? Helper.TrueArray : Helper.FalseArray);
        }
        [MessengerId((ushort)ReverseMessengerIds.DenysDelForward)]
        public async Task DenysDelForward(IConnection connection)
        {
            MasterDenyDelInfo info = serializer.Deserialize<MasterDenyDelInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo from) == false || from.Super == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }

            bool resp = await ReverseServerNodeReportTransfer.DenysDelForward(info).ConfigureAwait(false);
            connection.Write(resp ? Helper.TrueArray : Helper.FalseArray);
        }
        [MessengerId((ushort)ReverseMessengerIds.DenysDel)]
        public async Task DenysDel(IConnection connection)
        {
            MasterDenyDelInfo info = serializer.Deserialize<MasterDenyDelInfo>(connection.ReceiveRequestWrap.Payload.Span);
            bool resp = await ReverseServerNodeReportTransfer.DenysDel(connection, info).ConfigureAwait(false);
            connection.Write(resp ? Helper.TrueArray : Helper.FalseArray);
        }

        /// <summary>
        /// 信标服务器收到来自节点的连接
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)ReverseMessengerIds.ProxyForward)]
        public async Task ProxyForward(IConnection connection)
        {
            ReverseProxyInfo info = serializer.Deserialize<ReverseProxyInfo>(connection.ReceiveRequestWrap.Payload.Span);
            var node = await ReverseServerNodeReportTransfer.GetNode(info.NodeId).ConfigureAwait(false);
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
                MessengerId = (ushort)ReverseMessengerIds.Proxy,
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
            if (ReverseServerCahing.TryGet(host, out string machineId, out string nodeid) == false)
            {
                if (string.IsNullOrWhiteSpace(ReverseServerNodeReportTransfer.Config.Domain))
                {
                    return "Node domain not found";
                }
                host = host.Substring(0, host.Length - ReverseServerNodeReportTransfer.Config.Domain.Length - 1);
                if (ReverseServerCahing.TryGet(host, out machineId, out nodeid) == false)
                {
                    return "Host to machine not found";
                }
            }

            bool result = await ReverseServerNodeTransfer.ProxyForward(new ReverseProxyInfo
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
            ReverseServerCahing.TryGet(port, out string machineId, out string nodeid);
            if (string.IsNullOrWhiteSpace(machineId) || string.IsNullOrWhiteSpace(nodeid))
            {

            }

            bool result = await ReverseServerNodeTransfer.ProxyForward(new ReverseProxyInfo
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
            ReverseServerCahing.TryGet(port, out string machineId, out string nodeid);
            if (string.IsNullOrWhiteSpace(machineId) || string.IsNullOrWhiteSpace(nodeid))
            {

            }
            return await ReverseServerNodeTransfer.ProxyForward(new ReverseProxyInfo
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
                var dic = ReverseServerCahing.GetMachineIds();
                if (dic.Count == 0) return;

                foreach (var kv in dic)
                {
                    if (kv.Value.Count == 0) continue;

                    var allIds = kv.Value.Select(c => c.MachineId).ToList();
                    var offIds = await ReverseServerNodeTransfer.Heart(allIds, kv.Key);
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
    public sealed class ReverseClientMessenger : IMessenger
    {
        private readonly ReverseProxy proxy;
        private readonly ReverseClientTransfer ReverseTransfer;
        private readonly IReverseClientStore ReverseClientStore;
        private readonly ISerializer serializer;
        public ReverseClientMessenger(ReverseProxy proxy, ReverseClientTransfer ReverseTransfer, IReverseClientStore ReverseClientStore, ISerializer serializer)
        {
            this.proxy = proxy;
            this.ReverseTransfer = ReverseTransfer;
            this.ReverseClientStore = ReverseClientStore;
            this.serializer = serializer;
        }

        /// <summary>
        /// 别人来获取穿透记录
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)ReverseMessengerIds.Get)]
        public void Get(IConnection connection)
        {
            List<ReverseInfo> result = ReverseClientStore.Get().ToList();
            connection.Write(serializer.Serialize(result));
        }
        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="connection"></param>

        [MessengerId((ushort)ReverseMessengerIds.AddClient)]
        public void AddClient(IConnection connection)
        {
            ReverseInfo ReverseInfo = serializer.Deserialize<ReverseInfo>(connection.ReceiveRequestWrap.Payload.Span);
            ReverseTransfer.Add(ReverseInfo);
            connection.Write(Helper.TrueArray);
        }
        // <summary>
        /// 删除
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)ReverseMessengerIds.RemoveClient)]
        public void RemoveClient(IConnection connection)
        {
            int id = serializer.Deserialize<int>(connection.ReceiveRequestWrap.Payload.Span);
            ReverseTransfer.Remove(id);
            connection.Write(Helper.TrueArray);
        }
        [MessengerId((ushort)ReverseMessengerIds.StartClient)]
        public void StartClient(IConnection connection)
        {
            int id = serializer.Deserialize<int>(connection.ReceiveRequestWrap.Payload.Span);
            ReverseTransfer.Start(id);
        }
        [MessengerId((ushort)ReverseMessengerIds.StopClient)]
        public void StopClient(IConnection connection)
        {
            int id = serializer.Deserialize<int>(connection.ReceiveRequestWrap.Payload.Span);
            ReverseTransfer.Stop(id);
        }

        // <summary>
        /// 测试
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)ReverseMessengerIds.TestClient)]
        public void TestClient(IConnection connection)
        {
            ReverseTransfer.SubscribeTest();
        }


        /// <summary>
        /// 收到服务器发来的连接
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)ReverseMessengerIds.Proxy)]
        public void Proxy(IConnection connection)
        {
            ReverseProxyInfo info = serializer.Deserialize<ReverseProxyInfo>(connection.ReceiveRequestWrap.Payload.Span);
            IPEndPoint server = new IPEndPoint(IPAddress.Any.Equals(info.Addr) || IPAddress.Loopback.Equals(info.Addr) ? connection.Address.Address : info.Addr, info.RemotePort);
            //是http
            if (string.IsNullOrWhiteSpace(info.Domain) == false)
            {
                ReverseInfo ReverseInfo = ReverseClientStore.Get(info.Domain);
                if (ReverseInfo != null)
                {
                    _ = proxy.OnConnectTcp(info.Domain, info.BufferSize, info.Id, server, ReverseInfo.LocalEP);
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
        /// <param name="ReverseProxyInfo"></param>
        /// <returns></returns>
        private IPEndPoint GetLocalEP(ReverseProxyInfo ReverseProxyInfo)
        {
            ReverseInfo ReverseInfo = ReverseClientStore.Get().FirstOrDefault(c => c.RemotePort == ReverseProxyInfo.RemotePort || (c.RemotePortMin <= ReverseProxyInfo.RemotePort && c.RemotePortMax >= ReverseProxyInfo.RemotePort));
            if (ReverseInfo != null)
            {
                IPEndPoint localEP = IPEndPoint.Parse(ReverseInfo.LocalEP.ToString());
                if (ReverseInfo.RemotePortMin != 0 && ReverseInfo.RemotePortMax != 0)
                {
                    uint plus = (uint)(ReverseProxyInfo.RemotePort - ReverseInfo.RemotePortMin);
                    uint newIP = NetworkHelper.ToValue(localEP.Address) + plus;
                    localEP.Address = NetworkHelper.ToIP(newIP);
                }
                return localEP;
            }
            return null;
        }

    }
}
