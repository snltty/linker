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

        public ClientSignInTransfer(ClientSignInState clientSignInState, Config config, TcpServer tcpServer, MessengerSender messengerSender)
        {
            this.clientSignInState = clientSignInState;
            this.config = config;
            this.tcpServer = tcpServer;
            this.messengerSender = messengerSender;

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
            IPAddress[] ips = new IPAddress[] { config.Client.ServerEP.Address };

            if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                Logger.Instance.Info($"get ip:{ips.ToJsonFormat()}");

            if (ips.Length == 0) return;
            foreach (IPAddress ip in ips)
            {
                try
                {
                    IPEndPoint remote = new IPEndPoint(ip, config.Client.ServerEP.Port);
                    //Logger.Instance.Info($"connect server {remote}");
                    Socket socket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    socket.KeepAlive();
                    IAsyncResult result = socket.BeginConnect(remote, null, null);
                    await Task.Delay(500);
                    if (result.IsCompleted == false)
                    {
                        socket.SafeClose();
                        continue;
                    }
                    clientSignInState.Connection = tcpServer.BindReceive(socket);
                    MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
                    {
                        Connection = clientSignInState.Connection,
                        MessengerId = (ushort)SignInMessengerIds.SignIn,
                        Payload = MemoryPackSerializer.Serialize(new SignInfo
                        {
                            MachineName = config.Client.Name,
                            Version = config.Version
                        })
                    });
                    if (resp.Code != MessageResponeCodes.OK || resp.Data.Span.SequenceEqual(Helper.TrueArray) == false)
                    {
                        clientSignInState.Connection?.Disponse();
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

    }
}
