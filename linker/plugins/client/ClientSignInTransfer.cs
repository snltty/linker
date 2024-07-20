using linker.client.config;
using linker.config;
using linker.plugins.signin.messenger;
using linker.libs;
using linker.libs.extends;
using MemoryPack;
using System.Net;
using System.Net.Sockets;
using linker.plugins.client.args;
using linker.plugins.server;
using linker.plugins.messenger;

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
        private readonly RunningConfigTransfer runningConfigTransfer;

        private string configKey = "signServers";

        public ClientSignInTransfer(ClientSignInState clientSignInState, RunningConfig runningConfig, FileConfig config,  MessengerSender messengerSender, MessengerResolver messengerResolver, SignInArgsTransfer signInArgsTransfer, RunningConfigTransfer runningConfigTransfer)
        {
            this.clientSignInState = clientSignInState;
            this.runningConfig = runningConfig;
            this.config = config;
            this.messengerSender = messengerSender;
            this.messengerResolver = messengerResolver;
            this.signInArgsTransfer = signInArgsTransfer;
            this.runningConfigTransfer = runningConfigTransfer;

            if (string.IsNullOrWhiteSpace(config.Data.Client.Server) && runningConfig.Data.Client.Servers.Length > 0)
                config.Data.Client.Server = runningConfig.Data.Client.Servers.FirstOrDefault().Host;

            runningConfigTransfer.Setter(configKey, SetServers);
            runningConfigTransfer.Getter(configKey, () => MemoryPackSerializer.Serialize(runningConfig.Data.Client.Servers));

            clientSignInState.NetworkFirstEnabledHandle += () =>
            {
                SyncServers();
            };
        }

        /// <summary>
        /// 开始定期检查登入状态
        /// </summary>
        public void SignInTask()
        {
            Task.Run(async () =>
            {
                while (true)
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
                    await Task.Delay(10000).ConfigureAwait(false);
                }
            });
        }

        /// <summary>
        /// 登入
        /// </summary>
        /// <returns></returns>
        public async Task SignIn()
        {
            if (string.IsNullOrWhiteSpace(config.Data.Client.GroupId))
            {
                return;
            }
            if (BooleanHelper.CompareExchange(ref clientSignInState.connecting, true, false))
            {
                return;
            }

            try
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Info($"connect to signin server :{config.Data.Client.Server}");

                IPEndPoint ip = NetworkHelper.GetEndPoint(config.Data.Client.Server, 1802);

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
            Dictionary<string, string> args = new Dictionary<string, string>();
            signInArgsTransfer.Invoke(args);

            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)SignInMessengerIds.SignIn,
                Payload = MemoryPackSerializer.Serialize(new SignInfo
                {
                    MachineName = config.Data.Client.Name,
                    MachineId = config.Data.Client.Id,
                    Version = config.Data.Version,
                    Args = args,
                    GroupId = config.Data.Client.GroupId,
                })
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK)
            {
                if (resp.Data.Span.SequenceEqual(Helper.FalseArray) == false)
                {
                    config.Data.Client.Id = MemoryPackSerializer.Deserialize<string>(resp.Data.Span);
                    config.Data.Update();
                    return true;
                }
            }
            clientSignInState.Connection?.Disponse(6);
            return false;
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
                clientSignInState.Version = "v1.0.0.0";
            }
        }


        /// <summary>
        /// 修改客户端名称
        /// </summary>
        /// <param name="newName"></param>
        public void SetName(string newName)
        {
            string name = config.Data.Client.Name;

            if (name != newName)
            {
                config.Data.Client.Name = newName;
                config.Data.Update();

                SignOut();
                _ = SignIn();
            }


        }
        /// <summary>
        /// 修改客户端名称和分组编号
        /// </summary>
        /// <param name="newName"></param>
        /// <param name="newGroupid"></param>
        public void Set(string newName, string newGroupid)
        {
            string name = config.Data.Client.Name;
            string gid = config.Data.Client.GroupId;

            if (name != newName || gid != newGroupid)
            {
                config.Data.Client.Name = newName;
                config.Data.Client.GroupId = newGroupid;
                config.Data.Update();
                SignOut();
                _ = SignIn();
            }
        }

        /// <summary>
        /// 修改信标服务器列表
        /// </summary>
        /// <param name="servers"></param>
        public async Task SetServers(ClientServerInfo[] servers)
        {
            await SetServersReSignin(servers);
            runningConfigTransfer.IncrementVersion(configKey);
            SyncServers();
        }
        private void SetServers(Memory<byte> data)
        {
            _ = SetServersReSignin(MemoryPackSerializer.Deserialize<ClientServerInfo[]>(data.Span));
        }
        private async Task SetServersReSignin(ClientServerInfo[] servers)
        {
            string server = config.Data.Client.Server;
            runningConfig.Data.Client.Servers = servers;
            if (runningConfig.Data.Client.Servers.Length > 0)
            {
                config.Data.Client.Server = runningConfig.Data.Client.Servers.FirstOrDefault().Host;
            }
            runningConfig.Data.Update();
            if (server != config.Data.Client.Server)
            {
                SignOut();
                await SignIn();
            }
        }
        private void SyncServers()
        {
            runningConfigTransfer.Sync(configKey, MemoryPackSerializer.Serialize(runningConfig.Data.Client.Servers));
        }

    }
}
