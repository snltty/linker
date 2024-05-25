using cmonitor.client.args;
using cmonitor.config;
using cmonitor.plugins.signin.messenger;
using cmonitor.server;
using common.libs;
using common.libs.extends;
using MemoryPack;
using System.Net;
using System.Net.Sockets;

namespace cmonitor.client
{
    public sealed class ClientSignInTransfer
    {
        private readonly ClientSignInState clientSignInState;
        private readonly Config config;
        private readonly TcpServer tcpServer;
        private readonly MessengerSender messengerSender;
        private readonly SignInArgsTransfer signInArgsTransfer;

        public ClientSignInTransfer(ClientSignInState clientSignInState, Config config, TcpServer tcpServer, MessengerSender messengerSender, SignInArgsTransfer signInArgsTransfer)
        {
            this.clientSignInState = clientSignInState;
            this.config = config;
            this.tcpServer = tcpServer;
            this.messengerSender = messengerSender;
            this.signInArgsTransfer = signInArgsTransfer;

            SignInTask();
        }

        private void SignInTask()
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

        public async Task SignIn()
        {
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
        public void SignOut()
        {
            if (clientSignInState.Connected)
                clientSignInState.Connection.Disponse();
        }

        public void UpdateName(string newName)
        {
            config.Data.Client.Name = newName;
            config.Save();

            SignOut();
            _ = SignIn();
        }
        public void UpdateName(string newName, string newGroupid)
        {
            string name = config.Data.Client.Name;
            string gid = config.Data.Client.GroupId;

            config.Data.Client.Name = newName;
            config.Data.Client.GroupId = newGroupid;
            config.Save();

            if (name != config.Data.Client.Name || gid != config.Data.Client.GroupId)
            {
                SignOut();
                _ = SignIn();
            }
        }
        public void UpdateServers(ClientServerInfo[] servers)
        {
            string server = config.Data.Client.Server;

            config.Data.Client.Servers = servers;
            config.Save();

            if (server != config.Data.Client.Server)
            {
                SignOut();
                _ = SignIn();
            }
        }

        private async Task<bool> ConnectServer(IPEndPoint remote)
        {
            Socket socket = new Socket(remote.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.KeepAlive();
            await socket.ConnectAsync(remote).WaitAsync(TimeSpan.FromMilliseconds(5000)).ConfigureAwait(false);
            clientSignInState.Connection = await tcpServer.BeginReceive(socket);
            return true;
        }
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
                    Version = config.Data.Version,
                    Args = args,
                    GroupId = config.Data.Client.GroupId,
                })
            });
            if (resp.Code != MessageResponeCodes.OK || resp.Data.Span.SequenceEqual(Helper.TrueArray) == false)
            {
                clientSignInState.Connection?.Disponse();
                return false;
            }
            return true;
        }
    }
}
