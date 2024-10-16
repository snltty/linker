using linker.client.config;
using linker.config;
using linker.plugins.signin.messenger;
using linker.libs;
using linker.libs.extends;
using MemoryPack;
using System.Net;
using System.Net.Sockets;
using linker.plugins.messenger;
using linker.plugins.signIn.args;
using linker.plugins.signin;

namespace linker.plugins.client
{
    /// <summary>
    /// 登入
    /// </summary>
    public sealed class ClientSignInTransfer
    {
        private readonly ClientSignInState clientSignInState;
        private readonly RunningConfig runningConfig;
        private readonly FileConfig config;
        private readonly MessengerSender messengerSender;
        private readonly MessengerResolver messengerResolver;
        private readonly SignInArgsTransfer signInArgsTransfer;

        public ClientSignInTransfer(ClientSignInState clientSignInState, RunningConfig runningConfig, FileConfig config, MessengerSender messengerSender, MessengerResolver messengerResolver, SignInArgsTransfer signInArgsTransfer)
        {
            this.clientSignInState = clientSignInState;
            this.runningConfig = runningConfig;
            this.config = config;
            this.messengerSender = messengerSender;
            this.messengerResolver = messengerResolver;
            this.signInArgsTransfer = signInArgsTransfer;
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
            }, 10000);
        }

        /// <summary>
        /// 登入
        /// </summary>
        /// <returns></returns>
        public async Task SignIn()
        {
            if (string.IsNullOrWhiteSpace(config.Data.Client.Group.Id))
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
                    LoggerHelper.Instance.Info($"connect to signin server:{config.Data.Client.ServerInfo.Host}");

                IPEndPoint ip = NetworkHelper.GetEndPoint(config.Data.Client.ServerInfo.Host, 1802);
                if (ip == null)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        LoggerHelper.Instance.Error($"get domain ip fail:{config.Data.Client.ServerInfo.Host}");
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

                GCHelper.FlushMemory();
                clientSignInState.PushNetworkEnabled();

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
            clientSignInState.Connection = await messengerResolver.BeginReceiveClient(socket).ConfigureAwait(false);

            return true;
        }
        /// <summary>
        /// 登入到信标服务器
        /// </summary>
        /// <returns></returns>
        private async Task<bool> SignIn2Server()
        {
            Dictionary<string, string> args = [];
            string argResult = await signInArgsTransfer.Invoke(config.Data.Client.ServerInfo.Host, args);
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
                Payload = MemoryPackSerializer.Serialize(new SignInfo
                {
                    MachineName = config.Data.Client.Name,
                    MachineId = config.Data.Client.Id,
                    Version = config.Data.Version,
                    Args = args,
                    GroupId = config.Data.Client.Group.Id,
                })
            }).ConfigureAwait(false);
            if (resp.Code != MessageResponeCodes.OK)
            {
                LoggerHelper.Instance.Error($"sign in fail : {resp.Code}");
                clientSignInState.Connection?.Disponse(6);
                return false;
            }

            SignInResponseInfo signResp = MemoryPackSerializer.Deserialize<string>(resp.Data.Span).DeJson<SignInResponseInfo>();
            if (signResp.Status == false)
            {
                LoggerHelper.Instance.Error($"sign in fail : {signResp.Msg}");
                clientSignInState.Connection?.Disponse(6);
                return false;
            }

            config.Data.Client.Id = signResp.MachineId;
            config.Data.Update();
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
                clientSignInState.Version = MemoryPackSerializer.Deserialize<string>(resp.Data.Span);
            }
            else
            {
                clientSignInState.Version = "v1.0.0";
            }
        }


        /// <summary>
        /// 修改客户端名称
        /// </summary>
        /// <param name="newName"></param>
        public void Set(string newName)
        {
            config.Data.Client.Name = newName;
            config.Data.Update();
        }
        /// <summary>
        /// 修改客户端名称和分组编号
        /// </summary>
        /// <param name="newName"></param>
        /// <param name="groups"></param>
        public void Set(string newName, ClientGroupInfo[] groups)
        {
            config.Data.Client.Name = newName;
            config.Data.Client.Groups = groups.DistinctBy(c => c.Name).ToArray(); 
            config.Data.Update();
        }
        /// <summary>
        /// 设置分组编号
        /// </summary>
        /// <param name="groups"></param>
        public void Set(ClientGroupInfo[] groups)
        {
            config.Data.Client.Groups = groups.DistinctBy(c=>c.Name).ToArray();
            config.Data.Update();
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
                Payload = MemoryPackSerializer.Serialize(machineId),
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
                return MemoryPackSerializer.Deserialize<string>(resp.Data.Span);
            }

            return string.Empty;
        }

        /// <summary>
        /// 修改信标服务器列表
        /// </summary>
        /// <param name="servers"></param>
        public void SetServers(ClientServerInfo[] servers)
        {
            SetServersReSignin(servers);
        }
        private void SetServersReSignin(ClientServerInfo[] servers)
        {
            config.Data.Client.Servers = servers;
            config.Data.Update();
        }
    }
}
