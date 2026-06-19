using linker.libs;
using linker.libs.extends;
using linker.messenger.decenter;
using linker.messenger.reverse.messenger;
using linker.messenger.signin;
using System.Net.Sockets;

namespace linker.messenger.reverse.client
{
    public sealed class ReverseClientTransfer
    {
        public int Count => ReverseClientStore.Count();
        public Action OnChanged { get; set; } = () => { };
        public Action<long, string> OnOpen = (id, flag) => { };
        public Action<long, string> OnClose = (id, flag) => { };

        private readonly SignInClientState signInClientState;
        private readonly IMessengerSender messengerSender;
        private readonly IReverseClientStore ReverseClientStore;
        private readonly ISerializer serializer;
        private readonly ReverseClientTestTransfer ReverseClientTestTransfer;
        private readonly CounterDecenter counterDecenter;

        public ReverseClientTransfer(SignInClientState signInClientState, IMessengerSender messengerSender,
            IReverseClientStore ReverseClientStore, ISerializer serializer,
            ReverseClientTestTransfer ReverseClientTestTransfer, CounterDecenter counterDecenter)
        {
            this.signInClientState = signInClientState;
            this.messengerSender = messengerSender;
            this.ReverseClientStore = ReverseClientStore;

            this.serializer = serializer;
            this.ReverseClientTestTransfer = ReverseClientTestTransfer;
            this.counterDecenter = counterDecenter;
            counterDecenter.SetValue("reverse", Count);
        }

        public void Start(long id, string flag = "")
        {
            ReverseInfo forwardInfo = ReverseClientStore.Get(id);
            if (forwardInfo != null)
            {
                Start(forwardInfo, flag);
                OnChanged();
            }
        }
        public void Stop(long id, string flag = "")
        {
            ReverseInfo forwardInfo = ReverseClientStore.Get(id);
            if (forwardInfo != null)
            {
                Stop(forwardInfo, flag);
                OnChanged();
            }
        }
        private void Start(ReverseInfo forwardInfo, string flag = "")
        {
            if (forwardInfo.RemotePort == 0 && string.IsNullOrWhiteSpace(forwardInfo.Domain))
            {
                ReverseClientStore.Update(forwardInfo.Id, false, $"Please use port or domain");
                return;
            }
            try
            {
                if (ReverseClientTestTransfer.Nodes.Count == 0)
                {
                    _ = ReverseClientTestTransfer.TaskNodes();
                }

                forwardInfo.NodeId1 = string.IsNullOrWhiteSpace(forwardInfo.NodeId) || forwardInfo.NodeId == "*" ? ReverseClientTestTransfer.DefaultId() : forwardInfo.NodeId;
                ReverseClientStore.UpdateNodeId1(forwardInfo.Id, forwardInfo.NodeId1);

                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Info($"start Reverse {forwardInfo.ToJson()}");

                messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = signInClientState.Connection,
                    MessengerId = (ushort)ReverseMessengerIds.StartForward,
                    Payload = serializer.Serialize(new ReverseAddInfo { Domain = forwardInfo.Domain, RemotePort = forwardInfo.RemotePort, NodeId = forwardInfo.NodeId1 })
                }).ContinueWith((result) =>
                {
                    if (result.Result.Code == MessageResponeCodes.OK)
                    {
                        ReverseAddResultInfo ReverseAddResultInfo = serializer.Deserialize<ReverseAddResultInfo>(result.Result.Data.Span);
                        forwardInfo.BufferSize = ReverseAddResultInfo.BufferSize;
                        if (ReverseAddResultInfo.Success)
                        {
                            ReverseClientStore.Update(forwardInfo.Id, true, string.Empty);
                            LoggerHelper.Instance.Debug(ReverseAddResultInfo.Message);
                            OnOpen(forwardInfo.Id, flag);
                        }
                        else
                        {
                            ReverseClientStore.Update(forwardInfo.Id, false, ReverseAddResultInfo.Message);
                            LoggerHelper.Instance.Error(ReverseAddResultInfo.Message);
                        }
                    }
                    else
                    {
                        ReverseClientStore.Update(forwardInfo.Id, false, string.Empty);
                    }
                });
            }
            catch (Exception ex)
            {
                ReverseClientStore.Update(forwardInfo.Id, false, ex.Message);
                LoggerHelper.Instance.Error(ex);
            }

        }
        private void Stop(ReverseInfo forwardInfo, string flag = "")
        {
            try
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Info($"stop Reverse {forwardInfo.ToJson()}");
                messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = signInClientState.Connection,
                    MessengerId = (ushort)ReverseMessengerIds.StopForward,
                    Payload = serializer.Serialize(new ReverseAddInfo { Domain = forwardInfo.Domain, RemotePort = forwardInfo.RemotePort, NodeId = forwardInfo.NodeId1 })
                }).ContinueWith((result) =>
                {
                    OnClose(forwardInfo.Id, flag);
                    ReverseClientStore.Update(forwardInfo.Id, false, string.Empty);
                });
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
            }

        }

        public bool Add(ReverseInfo forwardInfo)
        {
            ReverseClientStore.Add(forwardInfo);
            OnChanged();
            counterDecenter.SetValue("reverse", Count);
            return true;
        }
        public bool Remove(int id)
        {
            Stop(id);
            ReverseClientStore.Remove(id);
            OnChanged();
            counterDecenter.SetValue("reverse", Count);
            return true;
        }

        private readonly OperatingManager testing = new OperatingManager();
        public void SubscribeTest()
        {
            if (testing.StartOperation() == false)
            {
                return;
            }

            IEnumerable<Task<bool>> tasks = ReverseClientStore.Get().Select(Connect);
            Task.WhenAll(tasks).ContinueWith((result) =>
            {
                testing.StopOperation();
                OnChanged();
            });

            async Task<bool> Connect(ReverseInfo info)
            {
                using CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
                Socket socket = new Socket(info.LocalEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    await socket.ConnectAsync(info.LocalEP, cts.Token).ConfigureAwait(false);
                    ReverseClientStore.Update(info.Id, string.Empty);
                    return true;
                }
                catch (Exception ex)
                {
                    ReverseClientStore.Update(info.Id, $"【TCP】{ex.Message}");
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
