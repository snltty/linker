using linker.libs;
using linker.libs.extends;
using linker.libs.timer;
using linker.messenger.decenter;
using linker.messenger.sforward.messenger;
using linker.messenger.signin;
using System.Net.Sockets;

namespace linker.messenger.sforward.client
{
    public sealed class SForwardClientTransfer
    {
        public int Count => sForwardClientStore.Count();
        public Action OnChanged { get; set; } = () => { };
        public Action<long, string> OnOpen = (id, flag) => { };
        public Action<long, string> OnClose = (id, flag) => { };

        private readonly SignInClientState signInClientState;
        private readonly IMessengerSender messengerSender;
        private readonly ISForwardClientStore sForwardClientStore;
        private readonly ISerializer serializer;
        private readonly SForwardClientTestTransfer sForwardClientTestTransfer;
        private readonly CounterDecenter counterDecenter;

        public SForwardClientTransfer(SignInClientState signInClientState, IMessengerSender messengerSender,
            ISForwardClientStore sForwardClientStore, ISerializer serializer,
            SForwardClientTestTransfer sForwardClientTestTransfer, CounterDecenter counterDecenter)
        {
            this.signInClientState = signInClientState;
            this.messengerSender = messengerSender;
            this.sForwardClientStore = sForwardClientStore;

            this.serializer = serializer;
            this.sForwardClientTestTransfer = sForwardClientTestTransfer;
            this.counterDecenter = counterDecenter;
            counterDecenter.SetValue("sforward", Count);
        }

        public void Start(long id, string flag = "")
        {
            SForwardInfo191 forwardInfo = sForwardClientStore.Get(id);
            if (forwardInfo != null)
            {
                Start(forwardInfo, flag);
                OnChanged();
            }
        }
        public void Stop(long id, string flag = "")
        {
            SForwardInfo191 forwardInfo = sForwardClientStore.Get(id);
            if (forwardInfo != null)
            {
                Stop(forwardInfo, flag);
                OnChanged();
            }
        }
        private void Start(SForwardInfo191 forwardInfo, string flag = "")
        {
            if (forwardInfo.RemotePort == 0 && string.IsNullOrWhiteSpace(forwardInfo.Domain))
            {
                sForwardClientStore.Update(forwardInfo.Id, false, $"Please use port or domain");
                return;
            }
            try
            {
                if (sForwardClientTestTransfer.Nodes.Count == 0)
                {
                    _ = sForwardClientTestTransfer.TaskNodes();
                }

                forwardInfo.NodeId1 = string.IsNullOrWhiteSpace(forwardInfo.NodeId) || forwardInfo.NodeId == "*" ? sForwardClientTestTransfer.DefaultId() : forwardInfo.NodeId;
                sForwardClientStore.UpdateNodeId1(forwardInfo.Id, forwardInfo.NodeId1);

                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Info($"start sforward {forwardInfo.ToJson()}");

                messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = signInClientState.Connection,
                    MessengerId = (ushort)SForwardMessengerIds.AddForward191,
                    Payload = serializer.Serialize(new SForwardAddInfo191 { Domain = forwardInfo.Domain, RemotePort = forwardInfo.RemotePort, NodeId = forwardInfo.NodeId1 })
                }).ContinueWith((result) =>
                {
                    if (result.Result.Code == MessageResponeCodes.OK)
                    {
                        SForwardAddResultInfo sForwardAddResultInfo = serializer.Deserialize<SForwardAddResultInfo>(result.Result.Data.Span);
                        forwardInfo.BufferSize = sForwardAddResultInfo.BufferSize;
                        if (sForwardAddResultInfo.Success)
                        {
                            sForwardClientStore.Update(forwardInfo.Id, true, string.Empty);
                            LoggerHelper.Instance.Debug(sForwardAddResultInfo.Message);
                            OnOpen(forwardInfo.Id, flag);
                        }
                        else
                        {
                            sForwardClientStore.Update(forwardInfo.Id, false, sForwardAddResultInfo.Message);
                            LoggerHelper.Instance.Error(sForwardAddResultInfo.Message);
                        }
                    }
                    else
                    {
                        sForwardClientStore.Update(forwardInfo.Id, false, string.Empty);
                    }
                });
            }
            catch (Exception ex)
            {
                sForwardClientStore.Update(forwardInfo.Id, false, ex.Message);
                LoggerHelper.Instance.Error(ex);
            }

        }
        private void Stop(SForwardInfo191 forwardInfo, string flag = "")
        {
            try
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Info($"stop sforward {forwardInfo.ToJson()}");
                messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = signInClientState.Connection,
                    MessengerId = (ushort)SForwardMessengerIds.RemoveForward191,
                    Payload = serializer.Serialize(new SForwardAddInfo191 { Domain = forwardInfo.Domain, RemotePort = forwardInfo.RemotePort, NodeId = forwardInfo.NodeId1 })
                }).ContinueWith((result) =>
                {
                    OnClose(forwardInfo.Id, flag);
                    sForwardClientStore.Update(forwardInfo.Id, false, string.Empty);
                });
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
            }

        }

        public bool Add(SForwardInfo191 forwardInfo)
        {
            sForwardClientStore.Add(forwardInfo);
            OnChanged();
            counterDecenter.SetValue("sforward", Count);
            return true;
        }
        public bool Remove(int id)
        {
            Stop(id);
            sForwardClientStore.Remove(id);
            OnChanged();
            counterDecenter.SetValue("sforward", Count);
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
                    await socket.ConnectAsync(info.LocalEP).WaitAsync(TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);
                    sForwardClientStore.Update(info.Id, string.Empty);
                    return true;
                }
                catch (Exception ex)
                {
                    sForwardClientStore.Update(info.Id, $"【TCP】{ex.Message}");
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
