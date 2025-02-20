using linker.libs;
using linker.libs.extends;
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

        private readonly IForwardClientStore forwardClientStore;
        private readonly ForwardProxy forwardProxy;
        private readonly SignInClientState signInClientState;
        private readonly IMessengerSender messengerSender;
        private readonly ISignInClientStore signInClientStore;
        private readonly ISerializer serializer;

        private readonly NumberSpaceUInt32 ns = new NumberSpaceUInt32();

        public ForwardTransfer(IForwardClientStore forwardClientStore, ForwardProxy forwardProxy, SignInClientState signInClientState, IMessengerSender messengerSender, ISignInClientStore signInClientStore, ISerializer serializer)
        {
            this.forwardClientStore = forwardClientStore;
            this.forwardProxy = forwardProxy;
            this.signInClientState = signInClientState;
            this.messengerSender = messengerSender;
            this.signInClientStore = signInClientStore;
            this.serializer = serializer;

            signInClientState.OnSignInSuccess += Reset;
        }

        string groupid = string.Empty;
        private void Reset(int times)
        {
            TimerHelper.Async(async () =>
            {
                if (groupid != signInClientStore.Group.Id)
                {
                    OnReset();
                    Stop();
                }
                groupid = signInClientStore.Group.Id;

                await Task.Delay(5000).ConfigureAwait(false);
                Start(false);
            });
        }

        private void Start(bool errorStop = true)
        {
            lock (this)
            {
                uint maxid = forwardClientStore.Count() > 0 ? forwardClientStore.Get().Max(c => c.Id) : 1;
                ns.Reset(maxid);

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
                    forwardClientStore.Update(item);
                }
                forwardClientStore.Confirm();
                OnChanged();
            }
        }
        private void Start(ForwardInfo forwardInfo, bool errorStop = true)
        {
            if (forwardInfo.Proxy == false)
            {
                try
                {
                    forwardProxy.Start(new System.Net.IPEndPoint(forwardInfo.BindIPAddress, forwardInfo.Port), forwardInfo.TargetEP, forwardInfo.MachineId, forwardInfo.BufferSize);
                    forwardInfo.Port = forwardProxy.LocalEndpoint.Port;

                    if (forwardInfo.Port > 0)
                    {
                        forwardInfo.Started = true;
                        forwardInfo.Proxy = true;
                        forwardInfo.Msg = string.Empty;
                        LoggerHelper.Instance.Debug($"start forward {forwardInfo.Port}->{forwardInfo.MachineId}->{forwardInfo.TargetEP}");
                    }
                    else
                    {
                        if (errorStop)
                        {
                            forwardInfo.Started = false;
                        }
                        forwardInfo.Msg = $"start forward {forwardInfo.Port}->{forwardInfo.MachineId}->{forwardInfo.TargetEP} fail";
                        LoggerHelper.Instance.Error(forwardInfo.Msg);
                    }
                }
                catch (Exception ex)
                {
                    if (errorStop)
                    {
                        forwardInfo.Started = false;
                    }
                    forwardInfo.Msg = $"{ex.Message},start forward {forwardInfo.Port}->{forwardInfo.MachineId}->{forwardInfo.TargetEP} fail";
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
                    forwardProxy.StopPort(forwardInfo.Port);
                    forwardInfo.Proxy = false;
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
            }
            OnChanged();
        }

        public List<ForwardInfo> Get()
        {
            return forwardClientStore.Get(signInClientStore.Group.Id);
        }
        public bool Add(ForwardInfo forwardInfo)
        {
            //同名或者同端口，但是ID不一样
            ForwardInfo old = forwardClientStore.Get().FirstOrDefault(c => (c.Port == forwardInfo.Port && c.Port != 0) && c.MachineId == forwardInfo.MachineId);
            if (old != null && old.Id != forwardInfo.Id) return false;

            if (forwardInfo.Id != 0)
            {
                old = forwardClientStore.Get(forwardInfo.Id);
                if (old == null) return false;

                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Debug($"update forward {old.ToJson()}->{forwardInfo.ToJson()}");

                old.BindIPAddress = forwardInfo.BindIPAddress;
                old.Port = forwardInfo.Port;
                old.Name = forwardInfo.Name;
                old.TargetEP = forwardInfo.TargetEP;
                old.MachineId = forwardInfo.MachineId;
                old.MachineName = forwardInfo.MachineName;
                old.Started = forwardInfo.Started;
                old.BufferSize = forwardInfo.BufferSize;
                old.GroupId = signInClientStore.Group.Id;
            }
            else
            {
                forwardInfo.Id = ns.Increment();
                forwardInfo.GroupId = signInClientStore.Group.Id;

                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Debug($"add forward {forwardInfo.ToJson()}");

                forwardClientStore.Add(forwardInfo);
            }
            forwardClientStore.Confirm();

            Start();

            return true;
        }
        public bool Remove(uint id)
        {
            //同名或者同端口，但是ID不一样
            ForwardInfo old = forwardClientStore.Get(id);
            if (old == null) return false;

            old.Started = false;

            forwardClientStore.Remove(old.Id);

            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Debug($"remove forward {old.ToJson()}");

            Start();
            forwardClientStore.Confirm();
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

            await Task.WhenAll(list.Select(Connect));
            testing.StopOperation();
            return true;

            async Task Connect(ForwardTestInfo info)
            {
                Socket socket = new Socket(info.Target.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    await socket.ConnectAsync(info.Target).WaitAsync(TimeSpan.FromMilliseconds(100));
                    info.Msg = string.Empty;
                }
                catch (Exception ex)
                {
                    info.Msg = ex.Message;
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

            var forwards = Get().GroupBy(c => c.MachineId).ToDictionary(c => c.Key, d => d.Select(d => new ForwardTestInfo { Target = d.TargetEP }).ToList());

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

                    var list = Get();
                    foreach (var item in tests)
                    {
                        foreach (var value in item.Value)
                        {
                            var forward = list.FirstOrDefault(c => c.MachineId == item.Key && c.TargetEP.Equals(value.Target));
                            if (forward != null)
                            {
                                forward.TargetMsg = value.Msg;
                            }
                        }
                    }

                    OnChanged();
                }
            });
        }

    }
}
