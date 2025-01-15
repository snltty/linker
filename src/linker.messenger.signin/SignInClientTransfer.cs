using linker.libs;
using linker.libs.extends;
using System.Net;
using System.Net.Sockets;
using linker.messenger.signin.args;

namespace linker.messenger.signin
{
    /// <summary>
    /// 登入
    /// </summary>
    public sealed class SignInClientTransfer
    {
        private readonly SignInClientState clientSignInState;
        private readonly IMessengerSender messengerSender;
        private readonly IMessengerResolver messengerResolver;
        private readonly SignInArgsTransfer signInArgsTransfer;
        private readonly ISignInClientStore signInClientStore;
        private readonly ISerializer serializer;

        public SignInClientTransfer(SignInClientState clientSignInState, IMessengerSender messengerSender, IMessengerResolver messengerResolver,
            SignInArgsTransfer signInArgsTransfer, ISignInClientStore signInClientStore, ISerializer serializer)
        {
            this.clientSignInState = clientSignInState;
            this.messengerSender = messengerSender;
            this.messengerResolver = messengerResolver;
            this.signInArgsTransfer = signInArgsTransfer;
            this.signInClientStore = signInClientStore;
            this.serializer = serializer;
        }

        /// <summary>
        /// 开始定期检查登入状态
        /// </summary>
        public void SignInTask()
        {
            TimerHelper.SetInterval(async () =>
            {
                if (clientSignInState.Connected == false)
                {
                    try
                    {
                        await SignIn().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                            LoggerHelper.Instance.Error(ex);
                    }
                }
                return true;
            }, () => 10000);
        }

        public void ReSignIn()
        {
            SignOut();
            _ = SignIn();
        }

        /// <summary>
        /// 登入
        /// </summary>
        /// <returns></returns>
        public async Task SignIn()
        {
            if (string.IsNullOrWhiteSpace(signInClientStore.Group.Id))
            {
                LoggerHelper.Instance.Error($"please configure group id");
                return;
            }
            if (BooleanHelper.CompareExchange(ref clientSignInState.connecting, true, false))
            {
                return;
            }

            try
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Info($"connect to signin server:{signInClientStore.Server.Host}");

                IPEndPoint ip = await NetworkHelper.GetEndPointAsync(signInClientStore.Server.Host, 1802);
                if (ip == null)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        LoggerHelper.Instance.Error($"get domain ip fail:{signInClientStore.Server.Host}");
                    return;
                }

                if (await ConnectServer(ip).ConfigureAwait(false) == false)
                {
                    return;
                }
                if (await SignIn2Server().ConfigureAwait(false) == false)
                {
                    return;
                }

                await GetServerVersion().ConfigureAwait(false);

                clientSignInState.PushNetworkEnabledBefore();
                clientSignInState.PushNetworkEnabled();

                GCHelper.FlushMemory();
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error(ex);
            }
            finally
            {
                BooleanHelper.CompareExchange(ref clientSignInState.connecting, false, true);
            }
        }
        /// <summary>
        /// 连接到信标服务器
        /// </summary>
        /// <param name="remote"></param>
        /// <returns></returns>
        private async Task<bool> ConnectServer(IPEndPoint remote)
        {
            Socket socket = new Socket(remote.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.KeepAlive();
            await socket.ConnectAsync(remote).WaitAsync(TimeSpan.FromMilliseconds(5000)).ConfigureAwait(false);
            clientSignInState.Connection = await messengerResolver.BeginReceiveClient(socket, true, (byte)ResolverType.Messenger).ConfigureAwait(false);

            return true;
        }
        /// <summary>
        /// 登入到信标服务器
        /// </summary>
        /// <returns></returns>
        private async Task<bool> SignIn2Server()
        {
            Dictionary<string, string> args = [];
            string argResult = await signInArgsTransfer.Invoke(signInClientStore.Server.Host, args);
            if (string.IsNullOrWhiteSpace(argResult) == false)
            {
                LoggerHelper.Instance.Error(argResult);
                clientSignInState.Connection?.Disponse(6);
                return false;
            }

            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)SignInMessengerIds.SignIn_V_1_3_1,
                Payload = serializer.Serialize(new SignInfo
                {
                    MachineName = signInClientStore.Name,
                    MachineId = signInClientStore.Id,
                    Version = VersionHelper.version,
                    Args = args,
                    GroupId = signInClientStore.Group.Id,
                })
            }).ConfigureAwait(false);
            if (resp.Code != MessageResponeCodes.OK)
            {
                LoggerHelper.Instance.Error($"sign in fail : {resp.Code}");
                clientSignInState.Connection?.Disponse(6);
                return false;
            }

            SignInResponseInfo signResp = serializer.Deserialize<string>(resp.Data.Span).DeJson<SignInResponseInfo>();
            if (signResp.Status == false)
            {
                LoggerHelper.Instance.Error($"sign in fail : {signResp.Msg}");
                clientSignInState.Connection?.Disponse(6);
                return false;
            }
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
            });

            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
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
            });
            if (resp.Code == MessageResponeCodes.OK)
            {
                return serializer.Deserialize<string>(resp.Data.Span);
            }

            return string.Empty;
        }

    }
}
