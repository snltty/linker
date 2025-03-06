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

            signInClientState.OnSignInSuccess += (i) => Start();
            this.serializer = serializer;
        }

        private void Start()
        {
            var list = sForwardClientStore.Get();

            foreach (var item in list)
            {
                if (item.Started)
                {
                    Start(item);
                }
                else
                {
                    Stop(item);
                }
                sForwardClientStore.Update(item);
                sForwardClientStore.Confirm();
            }
            OnChanged();
        }
        private void Start(SForwardInfo forwardInfo)
        {
            if (forwardInfo.Proxy) return;
            if (forwardInfo.RemotePort == 0 && string.IsNullOrWhiteSpace(forwardInfo.Domain))
            {
                forwardInfo.Msg = $"Please use port or domain";
                forwardInfo.Started = false;
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
                            forwardInfo.Proxy = true;
                            forwardInfo.Msg = string.Empty;
                            LoggerHelper.Instance.Debug(sForwardAddResultInfo.Message);
                        }
                        else
                        {
                            forwardInfo.Started = false;
                            forwardInfo.Msg = sForwardAddResultInfo.Message;
                            LoggerHelper.Instance.Error(sForwardAddResultInfo.Message);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                forwardInfo.Started = false;
                LoggerHelper.Instance.Error(ex);
            }
            OnChanged();
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
                                forwardInfo.Proxy = false;
                                LoggerHelper.Instance.Debug(sForwardAddResultInfo.Message);
                            }
                            else
                            {
                                forwardInfo.Started = true;
                                LoggerHelper.Instance.Error(sForwardAddResultInfo.Message);
                            }

                        }
                    });
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
            }
            OnChanged();
        }

        public bool Add(SForwardInfo forwardInfo)
        {
            //同名或者同端口，但是ID不一样
            SForwardInfo old = sForwardClientStore.Get().FirstOrDefault(c => forwardInfo.RemotePort > 0 && c.RemotePort == forwardInfo.RemotePort || string.IsNullOrWhiteSpace(forwardInfo.Domain) == false && c.Domain == forwardInfo.Domain);
            if (old != null && old.Id != forwardInfo.Id) return false;

            if (forwardInfo.Id != 0)
            {
                old = sForwardClientStore.Get(forwardInfo.Id);
                if (old == null) return false;

                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Info($"update sforward {old.ToJson()}->{forwardInfo.ToJson()}");

                old.RemotePort = forwardInfo.RemotePort;
                old.Name = forwardInfo.Name;
                old.LocalEP = forwardInfo.LocalEP;
                old.Domain = forwardInfo.Domain;
                old.Started = forwardInfo.Started;

                if (PortRange(forwardInfo.Domain, out int min, out int max))
                {
                    old.RemotePortMin = min;
                    old.RemotePortMax = max;
                }
                sForwardClientStore.Update(old);
            }
            else
            {
                if (PortRange(forwardInfo.Domain, out int min, out int max))
                {
                    forwardInfo.RemotePortMin = min;
                    forwardInfo.RemotePortMax = max;
                }
                sForwardClientStore.Add(forwardInfo);
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Info($"add sforward {forwardInfo.ToJson()}");
            }

            sForwardClientStore.Confirm();
            Start();

            return true;
        }
        public bool Remove(long id)
        {
            //同名或者同端口，但是ID不一样
            SForwardInfo old = sForwardClientStore.Get(id);
            if (old == null)
            {
                return false;
            }

            old.Started = false;
            Start();

            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Info($"remove sforward {old.ToJson()}");

            sForwardClientStore.Remove(id);
            sForwardClientStore.Confirm();
            return true;
        }
        private bool PortRange(string str, out int min, out int max)
        {
            min = 0; max = 0;

            if (string.IsNullOrWhiteSpace(str)) return false;

            string[] arr = str.Split('/');
            return arr.Length == 2 && int.TryParse(arr[0], out min) && int.TryParse(arr[1], out max);
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
                    info.LocalMsg = string.Empty;
                    return true;
                }
                catch (Exception ex)
                {
                    info.LocalMsg = ex.Message;
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
