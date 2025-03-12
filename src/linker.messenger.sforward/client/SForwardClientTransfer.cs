using linker.libs;
using linker.plugins.sforward.messenger;
using System.Net.Sockets;
using linker.libs.extends;
using linker.messenger.signin;

namespace linker.messenger.sforward.client
{
    public sealed class SForwardClientTransfer
    {
        public Action OnChanged { get; set; } = () => { };
        public Action<int> OnOpen = (id) => { };
        public Action<int> OnClose = (id) => { };

        private readonly SignInClientState signInClientState;
        private readonly IMessengerSender messengerSender;
        private readonly ISignInClientStore signInClientStore;
        private readonly ISForwardClientStore sForwardClientStore;
        private readonly ISerializer serializer;

        public SForwardClientTransfer(SignInClientState signInClientState, IMessengerSender messengerSender, ISignInClientStore signInClientStore, ISForwardClientStore sForwardClientStore, ISerializer serializer)
        {
            this.signInClientState = signInClientState;
            this.messengerSender = messengerSender;
            this.signInClientStore = signInClientStore;
            this.sForwardClientStore = sForwardClientStore;

            //也有可能是服务端重启导致重新上线，所以不能在首次登录启动，要每次登录都尝试添加一下
            signInClientState.OnSignInSuccess += (i) => Start();
            this.serializer = serializer;
        }

        private void Start()
        {
            foreach (var item in sForwardClientStore.Get())
            {
                if (item.Started)
                {
                    Start(item);
                }
                else
                {
                    Stop(item);
                }
            }
        }
        private void Start(SForwardInfo forwardInfo)
        {
            if (forwardInfo.Proxy) return;
            if (forwardInfo.RemotePort == 0 && string.IsNullOrWhiteSpace(forwardInfo.Domain))
            {
                sForwardClientStore.Update(forwardInfo.Id, false, forwardInfo.Proxy, $"Please use port or domain");
                return;
            }

            try
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Info($"start sforward {forwardInfo.ToJson()}");
                messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = signInClientState.Connection,
                    MessengerId = (ushort)SForwardMessengerIds.Add,
                    Payload = serializer.Serialize(new SForwardAddInfo { Domain = forwardInfo.Domain, RemotePort = forwardInfo.RemotePort, SecretKey = sForwardClientStore.SecretKey })
                }).ContinueWith((result) =>
                {
                    if (result.Result.Code == MessageResponeCodes.OK)
                    {
                        SForwardAddResultInfo sForwardAddResultInfo = serializer.Deserialize<SForwardAddResultInfo>(result.Result.Data.Span);
                        forwardInfo.BufferSize = sForwardAddResultInfo.BufferSize;
                        if (sForwardAddResultInfo.Success)
                        {
                            sForwardClientStore.Update(forwardInfo.Id, forwardInfo.Started,true, string.Empty);
                            LoggerHelper.Instance.Debug(sForwardAddResultInfo.Message);
                            OnOpen(forwardInfo.Id);
                        }
                        else
                        {
                            sForwardClientStore.Update(forwardInfo.Id, false, forwardInfo.Proxy, sForwardAddResultInfo.Message);
                            LoggerHelper.Instance.Error(sForwardAddResultInfo.Message);
                        }
                    }
                    OnChanged();
                });
            }
            catch (Exception ex)
            {
                sForwardClientStore.Update(forwardInfo.Id, false, ex.Message);
                LoggerHelper.Instance.Error(ex);
                OnChanged();
            }
           
        }
        private void Stop(SForwardInfo forwardInfo)
        {
            try
            {
                if (forwardInfo.Proxy)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        LoggerHelper.Instance.Info($"stop sforward {forwardInfo.ToJson()}");
                    messengerSender.SendReply(new MessageRequestWrap
                    {
                        Connection = signInClientState.Connection,
                        MessengerId = (ushort)SForwardMessengerIds.Remove,
                        Payload = serializer.Serialize(new SForwardAddInfo { Domain = forwardInfo.Domain, RemotePort = forwardInfo.RemotePort, SecretKey = sForwardClientStore.SecretKey })
                    }).ContinueWith((result) =>
                    {
                        if (result.Result.Code == MessageResponeCodes.OK)
                        {
                            SForwardAddResultInfo sForwardAddResultInfo = serializer.Deserialize<SForwardAddResultInfo>(result.Result.Data.Span);
                            if (sForwardAddResultInfo.Success)
                            {
                                sForwardClientStore.Update(forwardInfo.Id, forwardInfo.Started, false, string.Empty);
                                LoggerHelper.Instance.Debug(sForwardAddResultInfo.Message);
                                OnClose(forwardInfo.Id);
                            }
                            else
                            {
                                sForwardClientStore.Update(forwardInfo.Id, true, forwardInfo.Proxy, string.Empty);
                                LoggerHelper.Instance.Error(sForwardAddResultInfo.Message);
                            }

                        }
                        OnChanged();
                    });
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
            }
          
        }

        public bool Add(SForwardInfo forwardInfo)
        {
            sForwardClientStore.Add(forwardInfo);
            Start();
            return true;
        }
        public bool Remove(int id)
        {
            sForwardClientStore.Update(id, false);
            Start();

            sForwardClientStore.Remove(id);
            return true;
        }

        private readonly OperatingManager testing = new OperatingManager();
        public void SubscribeTest()
        {
            if (testing.StartOperation() == false)
            {
                return;
            }

            IEnumerable<Task<bool>> tasks = sForwardClientStore.Get().Select(Connect);
            Task.WhenAll(tasks).ContinueWith((result) =>
            {
                testing.StopOperation();
                OnChanged();
            });

            async Task<bool> Connect(SForwardInfo info)
            {
                Socket socket = new Socket(info.LocalEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    await socket.ConnectAsync(info.LocalEP).WaitAsync(TimeSpan.FromMilliseconds(500));
                    sForwardClientStore.Update(info.Id, string.Empty);
                    return true;
                }
                catch (Exception ex)
                {
                    sForwardClientStore.Update(info.Id, ex.Message);
                }
                finally
                {
                    socket.SafeClose();
                }
                return false;
            }
        }
    }
}
