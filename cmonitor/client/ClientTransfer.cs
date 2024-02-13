using cmonitor.service;
using cmonitor.service.messengers.sign;
using common.libs;
using common.libs.extends;
using MemoryPack;
using System.Net;
using System.Net.Sockets;

namespace cmonitor.client
{
    public sealed class ClientTransfer
    {
        private readonly ClientSignInState clientSignInState;
        private readonly Config config;
        private readonly TcpServer tcpServer;
        private readonly MessengerSender messengerSender;

        public ClientTransfer(ClientSignInState clientSignInState, Config config, TcpServer tcpServer, MessengerSender messengerSender)
        {
            this.clientSignInState = clientSignInState;
            this.config = config;
            this.tcpServer = tcpServer;
            this.messengerSender = messengerSender;

            if (config.IsCLient)
            {
                SignInTask();
                tcpServer.OnDisconnected += (hashcode) =>
                {
                    clientSignInState.PushNetworkDisabled();
                };
            }
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
            IPAddress[] ips = new IPAddress[] { config.Server };

            if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                Logger.Instance.Info($"get ip:{ips.ToJsonFormat()}");

            if (ips.Length == 0) return;
            foreach (IPAddress ip in ips)
            {
                try
                {
                    IPEndPoint remote = new IPEndPoint(ip, config.ServicePort);

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
                            MachineName = config.Name,
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
