using linker.libs;
using linker.libs.extends;
using linker.libs.timer;
using linker.messenger.decenter;
using linker.messenger.forward.proxy;
using linker.messenger.signin;
using System.Net.Sockets;

namespace linker.messenger.forward
{
    public sealed class ForwardTransfer
    {
        public int Count => forwardClientStore.Get().Count(c => c.GroupId == signInClientStore.Group.Id);
        public Action OnChanged { get; set; } = () => { };
        public Action OnReset { get; set; } = () => { };

        private readonly StringChangedManager stringChangedManager = new StringChangedManager();

        private readonly IForwardClientStore forwardClientStore;
        private readonly ForwardProxy forwardProxy;
        private readonly SignInClientState signInClientState;
        private readonly IMessengerSender messengerSender;
        private readonly ISignInClientStore signInClientStore;
        private readonly ISerializer serializer;
        private readonly CounterDecenter counterDecenter;
        public ForwardTransfer(IForwardClientStore forwardClientStore, ForwardProxy forwardProxy, SignInClientState signInClientState,
            IMessengerSender messengerSender, ISignInClientStore signInClientStore, ISerializer serializer, CounterDecenter counterDecenter)
        {
            this.forwardClientStore = forwardClientStore;
            this.forwardProxy = forwardProxy;
            this.signInClientState = signInClientState;
            this.messengerSender = messengerSender;
            this.signInClientStore = signInClientStore;
            this.serializer = serializer;
            this.counterDecenter = counterDecenter;
            counterDecenter.SetValue("forward", Count);

            signInClientState.OnSignInSuccess += Reset;
        }

        private void Reset(int times)
        {
            TimerHelper.Async(async () =>
            {
                if (stringChangedManager.Input(signInClientStore.Group.Id))
                {
                    OnReset();
                    Stop();
                }
                await Task.Delay(5000).ConfigureAwait(false);
                Start(false);
            });
        }

        private void Start(bool errorStop = true)
        {
            lock (this)
            {
                foreach (var item in forwardClientStore.Get(signInClientStore.Group.Id))
                {
                    if (item.Started)
                    {
                        Start(item, errorStop);
                    }
                    else
                    {
                        Stop(item);
                    }
                }
                OnChanged();
            }
        }
        private void Start(ForwardInfo forwardInfo, bool errorStop = true)
        {
            if (forwardInfo.Proxy == false)
            {
                try
                {
                    forwardProxy.StartForward(new System.Net.IPEndPoint(forwardInfo.BindIPAddress, forwardInfo.Port), forwardInfo.TargetEP, forwardInfo.MachineId, forwardInfo.BufferSize);

                    forwardInfo.Port = forwardProxy.LocalEndpoint.Port;
                    forwardClientStore.Update(forwardInfo.Id, forwardInfo.Port);

                    if (forwardInfo.Port > 0)
                    {
                        forwardClientStore.Update(forwardInfo.Id, true, true, string.Empty);
                        LoggerHelper.Instance.Debug($"start forward {forwardInfo.Port}->{forwardInfo.MachineId}->{forwardInfo.TargetEP}");
                    }
                    else
                    {
                        if (errorStop)
                        {
                            forwardClientStore.Update(forwardInfo.Id, false);
                        }
                        forwardClientStore.Update(forwardInfo.Id, $"start forward {forwardInfo.Port}->{forwardInfo.MachineId}->{forwardInfo.TargetEP} fail");
                        LoggerHelper.Instance.Error(forwardInfo.Msg);
                    }
                }
                catch (Exception ex)
                {
                    if (errorStop)
                    {
                        forwardClientStore.Update(forwardInfo.Id, false);
                    }
                    forwardClientStore.Update(forwardInfo.Id, $"{ex.Message},start forward {forwardInfo.Port}->{forwardInfo.MachineId}->{forwardInfo.TargetEP} fail");
                    LoggerHelper.Instance.Error(ex);
                }
            }

            OnChanged();
        }

        private void Stop()
        {
            lock (this)
            {
                foreach (var item in forwardClientStore.Get())
                {
                    Stop(item);
                }
            }
        }
        private void Stop(ForwardInfo forwardInfo)
        {
            try
            {
                if (forwardInfo.Proxy)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        LoggerHelper.Instance.Debug($"stop forward {forwardInfo.ToJson()}");
                    forwardProxy.StopForward(forwardInfo.Port);
                    forwardInfo.Proxy = false;
                    forwardClientStore.Update(forwardInfo.Id, forwardInfo.Started, false, forwardInfo.Msg);
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
            }
            OnChanged();
        }

        public IEnumerable<ForwardInfo> Get()
        {
            return forwardClientStore.Get(signInClientStore.Group.Id);
        }
        public bool Add(ForwardInfo forwardInfo)
        {
            forwardInfo.GroupId = signInClientStore.Group.Id;
            forwardClientStore.Add(forwardInfo);
            counterDecenter.SetValue("forward",Count);
            Start();

            return true;
        }
        public bool Remove(int id)
        {
            forwardClientStore.Update(id, false);
            Start();
            forwardClientStore.Remove(id);
            counterDecenter.SetValue("forward", Count);
            return true;
        }

        private readonly OperatingManager testing = new OperatingManager();
        private readonly OperatingManager subing = new OperatingManager();
        public async Task<bool> Test(List<ForwardTestInfo> list)
        {
            if (testing.StartOperation() == false)
            {
                return false;
            }

            await Task.WhenAll(list.Select(Connect)).ConfigureAwait(false);
            testing.StopOperation();
            return true;

            async Task Connect(ForwardTestInfo info)
            {
                Socket socket = new Socket(info.Target.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    await socket.ConnectAsync(info.Target).WaitAsync(TimeSpan.FromMilliseconds(100)).ConfigureAwait(false);
                    info.Msg = string.Empty;
                }
                catch (Exception ex)
                {
                    info.Msg = $"【TCP】{ex.Message}";
                }
                finally
                {
                    socket.SafeClose();
                }
            }

        }
        public void SubscribeTest()
        {
            if (subing.StartOperation() == false)
            {
                return;
            }

            var forwards = Get().Where(c => string.IsNullOrWhiteSpace(c.MachineId) == false).GroupBy(c => c.MachineId).ToDictionary(c => c.Key, d => d.Select(d => new ForwardTestInfo { Target = d.TargetEP }).ToList());

            messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)ForwardMessengerIds.TestForward,
                Timeout = 5000,
                Payload = serializer.Serialize(forwards)
            }).ContinueWith((result) =>
            {
                subing.StopOperation();
                if (result.Result.Code == MessageResponeCodes.OK)
                {
                    Dictionary<string, List<ForwardTestInfo>> tests = serializer.Deserialize<Dictionary<string, List<ForwardTestInfo>>>(result.Result.Data.Span);

                    foreach (var item in tests)
                    {
                        foreach (var value in item.Value)
                        {
                            forwardClientStore.Update(item.Key, value.Target, value.Msg);
                        }
                    }

                    OnChanged();
                }
            });
        }

    }
}
