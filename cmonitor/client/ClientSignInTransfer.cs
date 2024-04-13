using cmonitor.client.args;
using cmonitor.config;
using cmonitor.plugins.signIn.messenger;
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
            tcpServer.OnDisconnected += (hashcode) =>
            {
                Logger.Instance.Info($"client disconnected");
                clientSignInState.PushNetworkDisabled();
            };
        }

        private void SignInTask()
        {
            Task.Factory.StartNew(async () =>
            {
                await Task.Delay(10000);
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
            }, TaskCreationOptions.LongRunning);
        }
        private async Task SignIn()
        {
            IPEndPoint[] ips = new IPEndPoint[] { config.Data.Client.ServerEP };

            if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                Logger.Instance.Info($"get ip:{ips.ToJsonFormat()}");

            foreach (IPEndPoint ip in ips)
            {
                try
                {
                    if (await ConnectServer(ip) == false)
                    {
                        continue;
                    }
                    if (await SignIn2Server() == false)
                    {
                        continue;
                    }

                    GCHelper.FlushMemory();
                    clientSignInState.PushNetworkEnabled();

                    break;
                }
                catch (Exception ex)
                {
                    if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        Logger.Instance.Error(ex);
                }
            }
        }

        private async Task<bool> ConnectServer(IPEndPoint remote)
        {
            Socket socket = new Socket(remote.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.KeepAlive();
            IAsyncResult result = socket.BeginConnect(remote, null, null);
            await Task.Delay(500);
            if (result.IsCompleted == false)
            {
                socket.SafeClose();
                return false;
            }
            clientSignInState.Connection = tcpServer.BindReceive(socket);
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
