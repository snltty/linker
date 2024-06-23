using linker.client.args;
using linker.client.config;
using linker.config;
using linker.plugins.signin.messenger;
using linker.server;
using linker.libs;
using linker.libs.extends;
using MemoryPack;
using System.Net;
using System.Net.Sockets;

namespace linker.client
{
    /// <summary>
    /// 登入
    /// </summary>
    public sealed class ClientSignInTransfer
    {
        private readonly ClientSignInState clientSignInState;
        private readonly RunningConfig runningConfig;
        private readonly Config config;
        private readonly TcpServer tcpServer;
        private readonly MessengerSender messengerSender;
        private readonly SignInArgsTransfer signInArgsTransfer;

        public ClientSignInTransfer(ClientSignInState clientSignInState, RunningConfig runningConfig, Config config, TcpServer tcpServer, MessengerSender messengerSender, SignInArgsTransfer signInArgsTransfer)
        {
            this.clientSignInState = clientSignInState;
            this.runningConfig = runningConfig;
            this.config = config;
            this.tcpServer = tcpServer;
            this.messengerSender = messengerSender;
            this.signInArgsTransfer = signInArgsTransfer;

            if (string.IsNullOrWhiteSpace(config.Data.Client.Server) && runningConfig.Data.Client.Servers.Length > 0)
                config.Data.Client.Server = runningConfig.Data.Client.Servers.FirstOrDefault().Host;
            //SignInTask();
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
                            await SignIn();
                        }
                        catch (Exception ex)
                        {
                            if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                                Logger.Instance.Error(ex);
                        }
                    }
                    await Task.Delay(10000);
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
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    Logger.Instance.Info($"connect to signin server :{config.Data.Client.Server}");

                IPEndPoint ip = NetworkHelper.GetEndPoint(config.Data.Client.Server, 1802);

                if (await ConnectServer(ip) == false)
                {
                    return;
                }
                if (await SignIn2Server() == false)
                {
                    return;
                }

                GCHelper.FlushMemory();
                clientSignInState.PushNetworkEnabled();

            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    Logger.Instance.Error(ex);
            }
            finally
            {
                BooleanHelper.CompareExchange(ref clientSignInState.connecting, false, true);
            }
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
        /// 修改客户端名称
        /// </summary>
        /// <param name="newName"></param>
        public void UpdateName(string newName)
        {
            string name = config.Data.Client.Name;

            if (name != newName)
            {
                config.Data.Client.Name = newName;
                config.Save();

                SignOut();
                _ = SignIn();
            }


        }
        /// <summary>
        /// 修改客户端名称和分组编号
        /// </summary>
        /// <param name="newName"></param>
        /// <param name="newGroupid"></param>
        public void UpdateName(string newName, string newGroupid)
        {
            string name = config.Data.Client.Name;
            string gid = config.Data.Client.GroupId;

            if (name != newName || gid != newGroupid)
            {
                config.Data.Client.Name = newName;
                config.Data.Client.GroupId = newGroupid;
                config.Save();
                SignOut();
                _ = SignIn();
            }
        }
        /// <summary>
        /// 修改信标服务器列表
        /// </summary>
        /// <param name="servers"></param>
        public void UpdateServers(ClientServerInfo[] servers)
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
                _ = SignIn();
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
            clientSignInState.Connection = await tcpServer.BeginReceive(socket);

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
            });
            if (resp.Code == MessageResponeCodes.OK)
            {
                if (resp.Data.Span.SequenceEqual(Helper.FalseArray) == false)
                {
                    config.Data.Client.Id = MemoryPackSerializer.Deserialize<string>(resp.Data.Span);
                    config.Save();
                    return true;
                }
            }
            clientSignInState.Connection?.Disponse(6);
            return false;
        }
    }
}
