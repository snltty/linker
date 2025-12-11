using linker.libs;
using linker.libs.extends;
using System.Net;
using System.Net.Sockets;
using linker.messenger.signin.args;
using linker.libs.timer;

namespace linker.messenger.signin
{
    /// <summary>
    /// 登入
    /// </summary>
    public sealed class SignInClientTransfer
    {
        private readonly OperatingManager operatingManager = new OperatingManager();

        private readonly SignInClientState clientSignInState;
        private readonly IMessengerSender messengerSender;
        private readonly IMessengerResolver messengerResolver;
        private readonly SignInArgsTransfer signInArgsTransfer;
        private readonly ISignInClientStore signInClientStore;
        private readonly ISerializer serializer;
        private readonly ICommonStore commonStore;

        public SignInClientTransfer(SignInClientState clientSignInState, IMessengerSender messengerSender, IMessengerResolver messengerResolver,
            SignInArgsTransfer signInArgsTransfer, ISignInClientStore signInClientStore, ISerializer serializer, ICommonStore commonStore)
        {
            this.clientSignInState = clientSignInState;
            this.messengerSender = messengerSender;
            this.messengerResolver = messengerResolver;
            this.signInArgsTransfer = signInArgsTransfer;
            this.signInClientStore = signInClientStore;
            this.serializer = serializer;
            this.commonStore = commonStore;
        }

        /// <summary>
        /// 开始定期检查登入状态
        /// </summary>
        public void SignInTask()
        {
            TimerHelper.SetIntervalLong(async () =>
            {
                try
                {
                    if (clientSignInState.Connected == false)
                    {
                        //未连接，按顺序尝试
                        string[] hosts = [signInClientStore.Server.Host, signInClientStore.Server.Host1, .. signInClientStore.Hosts];
                        foreach (var host in hosts.Where(c => string.IsNullOrWhiteSpace(c) == false))
                        {
                            if (await SignIn(host).ConfigureAwait(false) != 3)
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        //已连接，延期，测试主服务器，如果主服务器能连就切回主服务器
                        await Exp().ConfigureAwait(false);
                        if (await TestHost(signInClientStore.Server.Host))
                        {
                            await SignIn(signInClientStore.Server.Host).ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        LoggerHelper.Instance.Error(ex);
                }

            }, 10000);
        }
        private async Task<bool> TestHost(string host)
        {
            if (clientSignInState.SignInHost == signInClientStore.Server.Host) return false;
            try
            {
                IPEndPoint ip = await NetworkHelper.GetEndPointAsync(host, 1802).ConfigureAwait(false);
                using Socket socket = new Socket(ip.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                socket.KeepAlive();
                await socket.ConnectAsync(ip).WaitAsync(TimeSpan.FromMilliseconds(5000)).ConfigureAwait(false);

                socket.SafeClose();
                return true;
            }
            catch (Exception)
            {
            }
            return false;
        }

        /// <summary>
        /// 重新登录
        /// </summary>
        public void ReSignIn()
        {
            SignOut();
            _ = SignIn(signInClientStore.Server.Host);
        }

        /// <summary>
        /// 登入
        /// </summary>
        /// <returns></returns>
        private async Task<int> SignIn(string host)
        {
            if(commonStore.Installed == false)
            {
                LoggerHelper.Instance.Error($"not initialized");
                return 1;
            }
            if (string.IsNullOrWhiteSpace(signInClientStore.Group.Id))
            {
                LoggerHelper.Instance.Error($"group id are empty");
                return 1;
            }

            if (operatingManager.StartOperation() == false)
            {
                return 2;
            }

            try
            {
                await clientSignInState.PushSignInBefore().ConfigureAwait(false);

                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Info($"connect to signin server:{host}");

                IPEndPoint ip = await NetworkHelper.GetEndPointAsync(host, 1802).ConfigureAwait(false);
                if (ip == null)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        LoggerHelper.Instance.Error($"get domain ip fail:{host}");
                    return 3;
                }

                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Info($"connect to signin server:{ip}");

                Socket socket = await ConnectServer(ip).ConfigureAwait(false);
                if (socket == null)
                {
                    return 3;
                }
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Info($"connect to signin server success:{host}");

                if (await SignIn2Server(host, socket).ConfigureAwait(false) == false)
                {
                    return 3;
                }
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Info($"signin to server success:{host}");

                await GetServerVersion().ConfigureAwait(false);
                await CheckSuper().ConfigureAwait(false);

                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Info($"get server version:{clientSignInState.Version}");

                clientSignInState.PushSignInSuccessBefore();
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Info($"push signin success before");

                clientSignInState.PushSignInSuccess();

                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Info($"push signin success");

                GCHelper.FlushMemory();

                clientSignInState.SignInHost = host;
                return 0;
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error(ex);
                return 4;
            }
            finally
            {
                operatingManager.StopOperation();
            }
        }
        /// <summary>
        /// 连接到信标服务器
        /// </summary>
        /// <param name="remote"></param>
        /// <returns></returns>
        private async Task<Socket> ConnectServer(IPEndPoint remote)
        {
            try
            {
                Socket socket = new Socket(remote.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                socket.KeepAlive();
                await socket.ConnectAsync(remote).WaitAsync(TimeSpan.FromMilliseconds(5000)).ConfigureAwait(false);

                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Info($"begin recv signin connection");
                return socket;
            }
            catch (Exception)
            {
            }
            return null;
        }
        /// <summary>
        /// 登入到信标服务器
        /// </summary>
        /// <returns></returns>
        private async Task<bool> SignIn2Server(string host, Socket socket)
        {
            IConnection connection = await messengerResolver.BeginReceiveClient(socket, true, (byte)ResolverType.Messenger, Helper.EmptyArray).ConfigureAwait(false);

            Dictionary<string, string> args = [];
            string argResult = await signInArgsTransfer.Invoke(host, args).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(argResult) == false)
            {
                LoggerHelper.Instance.Error(argResult);
                connection?.Disponse(6);
                return false;
            }

            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = connection,
                MessengerId = (ushort)SignInMessengerIds.SignIn_V_1_3_1,
                Payload = serializer.Serialize(new SignInfo
                {
                    MachineName = signInClientStore.Name,
                    MachineId = signInClientStore.Id,
                    Version = VersionHelper.Version,
                    Args = args,
                    GroupId = signInClientStore.Group.Id,
                })
            }).ConfigureAwait(false);
            if (resp.Code != MessageResponeCodes.OK)
            {
                LoggerHelper.Instance.Error($"sign in fail : {resp.Code}");
                connection?.Disponse(6);
                return false;
            }

            SignInResponseInfo signResp = serializer.Deserialize<string>(resp.Data.Span).DeJson<SignInResponseInfo>();
            if (signResp.Status == false)
            {
                LoggerHelper.Instance.Error($"sign in fail : {signResp.Msg}");
                connection?.Disponse(6);
                return false;
            }
            clientSignInState.Connection = connection;
            clientSignInState.Connection.Id = signResp.MachineId;
            clientSignInState.Connection.Name = signInClientStore.Name;
            clientSignInState.WanAddress = signResp.IP;
            signInClientStore.SetId(signResp.MachineId);
            return true;
        }

        /// <summary>
        /// 登出
        /// </summary>
        public void SignOut()
        {
            if (clientSignInState.Connected)
                clientSignInState.Connection.Disponse(5);
        }

        /// <summary>
        /// 获取服务器版本
        /// </summary>
        /// <returns></returns>
        private async Task GetServerVersion()
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)SignInMessengerIds.Version,
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK)
            {
                clientSignInState.Version = serializer.Deserialize<string>(resp.Data.Span);
            }
            else
            {
                clientSignInState.Version = "v1.0.0";
            }
        }

        /// <summary>
        /// 检查超级权限
        /// </summary>
        /// <returns></returns>
        public async Task CheckSuper()
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)SignInMessengerIds.CheckSuper,
                Payload = serializer.Serialize(new KeyValuePair<string, string>(signInClientStore.Server.SuperKey, signInClientStore.Server.SuperPassword))
            }).ConfigureAwait(false);
            clientSignInState.Super = resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }

        /// <summary>
        /// 获取是否在线
        /// </summary>
        /// <param name="machineId"></param>
        /// <returns></returns>
        public async Task<bool> GetOnline(string machineId)
        {
            if (string.IsNullOrWhiteSpace(machineId))
            {
                return false;
            }

            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)SignInMessengerIds.Online,
                Payload = serializer.Serialize(machineId),
                Timeout = 3000
            }).ConfigureAwait(false);

            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }
        /// <summary>
        /// 获取离线列表
        /// </summary>
        /// <param name="machineIds"></param>
        /// <returns></returns>
        public async Task<List<string>> GetOfflines(List<string> machineIds)
        {
            if (machineIds == null || machineIds.Count == 0)
            {
                return [];
            }

            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)SignInMessengerIds.Offlines,
                Payload = serializer.Serialize(machineIds),
                Timeout = 3000
            }).ConfigureAwait(false);

            if (resp.Code == MessageResponeCodes.OK)
            {
                return serializer.Deserialize<List<string>>(resp.Data.Span);
            }
            return [];
        }

        /// <summary>
        /// 获取一个新的id
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetNewId()
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)SignInMessengerIds.NewId,
                Timeout = 3000
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK)
            {
                return serializer.Deserialize<string>(resp.Data.Span);
            }

            return string.Empty;
        }

        /// <summary>
        /// 延期
        /// </summary>
        /// <returns></returns>
        public async Task Exp()
        {

            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)SignInMessengerIds.Exp,
                Timeout = 3000
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK && resp.Data.Length > 0 && clientSignInState.SignInHost == signInClientStore.Server.Host)
            {
                var hosts = serializer.Deserialize<string[]>(resp.Data.Span);
                if (hosts != null && hosts.Length > 0)
                    signInClientStore.SetHosts(hosts);
            }
        }

        /// <summary>
        /// 客户端列表
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<SignInListResponseInfo> List(SignInListRequestInfo request)
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)SignInMessengerIds.List,
                Payload = serializer.Serialize(request)
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK)
            {
                return serializer.Deserialize<SignInListResponseInfo>(resp.Data.Span);
            }
            return new SignInListResponseInfo { };
        }
    }
}
