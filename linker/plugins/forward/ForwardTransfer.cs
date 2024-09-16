using linker.client.config;
using linker.libs;
using linker.libs.extends;
using linker.plugins.client;
using linker.plugins.forward.messenger;
using linker.plugins.forward.proxy;
using linker.plugins.messenger;
using linker.plugins.signin.messenger;
using MemoryPack;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace linker.plugins.forward
{
    public sealed class ForwardTransfer
    {
        private readonly RunningConfig running;
        private readonly ForwardProxy forwardProxy;
        private readonly ClientSignInState clientSignInState;
        private readonly MessengerSender messengerSender;

        private readonly NumberSpaceUInt32 ns = new NumberSpaceUInt32();

        public VersionManager Version { get; } = new VersionManager();

        public ForwardTransfer(RunningConfig running, ForwardProxy forwardProxy, ClientSignInState clientSignInState, MessengerSender messengerSender)
        {
            this.running = running;
            this.forwardProxy = forwardProxy;
            this.clientSignInState = clientSignInState;
            this.messengerSender = messengerSender;

            clientSignInState.NetworkEnabledHandle += Reset;
        }

        private void Reset(int times)
        {
            TimerHelper.Async(async () =>
            {
                await TestListen();
                Stop();
                await Task.Delay(5000).ConfigureAwait(false);
                Start();
            });
        }

        private void Start()
        {
            uint maxid = running.Data.Forwards.Count > 0 ? running.Data.Forwards.Max(c => c.Id) : 1;
            ns.Reset(maxid);

            foreach (var item in running.Data.Forwards)
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
        private void Start(ForwardInfo forwardInfo)
        {
            if (forwardInfo.Proxy == false)
            {
                try
                {
                    forwardProxy.Start(new System.Net.IPEndPoint(forwardInfo.BindIPAddress, forwardInfo.Port), forwardInfo.TargetEP, forwardInfo.MachineId, forwardInfo.BufferSize);
                    forwardInfo.Port = forwardProxy.LocalEndpoint.Port;

                    if (forwardInfo.Port > 0)
                    {
                        forwardInfo.Proxy = true;
                        forwardInfo.Msg = string.Empty;
                        LoggerHelper.Instance.Debug($"start forward {forwardInfo.Port}->{forwardInfo.MachineId}->{forwardInfo.TargetEP}");
                    }
                    else
                    {
                        forwardInfo.Msg = $"start forward {forwardInfo.Port}->{forwardInfo.MachineId}->{forwardInfo.TargetEP} fail";
                        LoggerHelper.Instance.Error(forwardInfo.Msg);
                    }

                }
                catch (Exception ex)
                {
                    forwardInfo.Started = false;
                    forwardInfo.Msg = ex.Message;
                    LoggerHelper.Instance.Error(ex);
                }
            }
            Version.Add();
        }

        private void Stop()
        {
            foreach (var item in running.Data.Forwards)
            {
                Stop(item);
            }
        }
        private void Stop(ForwardInfo forwardInfo)
        {
            try
            {
                if (forwardInfo.Proxy)
                {
                    LoggerHelper.Instance.Debug($"stop forward {forwardInfo.Port}->{forwardInfo.MachineId}->{forwardInfo.TargetEP}");
                    forwardProxy.StopPort(forwardInfo.Port);
                    forwardInfo.Proxy = false;
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
            }
            Version.Add();
        }


        ConcurrentDictionary<string, bool> testingDic = new ConcurrentDictionary<string, bool>();
        public void TestTarget()
        {
            foreach (var item in running.Data.Forwards.Select(c => c.MachineId).Distinct())
            {
                TestTarget(item);
            }
        }
        public void TestTarget(string machineId)
        {
            if (testingDic.TryAdd(machineId, true) == false) return;

            try
            {
                var endpoints = running.Data.Forwards.Where(c => c.MachineId == machineId).Select(c => c.TargetEP).ToList();
                if (endpoints.Count == 0)
                {
                    testingDic.TryRemove(machineId, out _);
                    return;
                }

                messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = clientSignInState.Connection,
                    MessengerId = (ushort)ForwardMessengerIds.TestForward,
                    Timeout = 2000,
                    Payload = MemoryPackSerializer.Serialize(new ForwardTestInfo
                    {
                        MachineId = machineId,
                        EndPoints = endpoints
                    })
                }).ContinueWith((result) =>
                {
                    testingDic.TryRemove(machineId, out _);

                    if (result.Result.Code != MessageResponeCodes.OK) return;

                    Dictionary<IPEndPoint, string> endpoints = MemoryPackSerializer.Deserialize<Dictionary<IPEndPoint, string>>(result.Result.Data.Span);

                    foreach (var item in running.Data.Forwards.Where(c => c.MachineId == machineId))
                    {
                        if (endpoints.TryGetValue(item.TargetEP, out string msg))
                        {
                            item.TargetMsg = msg;
                        }
                        else
                        {
                            item.TargetMsg = string.Empty;
                        }
                    }
                });
            }
            catch (Exception)
            {
                testingDic.TryRemove(machineId, out _);
            }
            Version.Add();
        }
        public async Task<Dictionary<IPEndPoint, string>> Test(ForwardTestInfo forwardTestInfo)
        {
            var results = forwardTestInfo.EndPoints.Select(ConnectAsync);
            await Task.Delay(200).ConfigureAwait(false);
            return results.Select(c => c.Result).Where(c => string.IsNullOrWhiteSpace(c.Item2) == false).ToDictionary(c => c.Item1, d => d.Item2);

            async Task<(IPEndPoint, string)> ConnectAsync(IPEndPoint ep)
            {
                try
                {
                    using Socket socket = new Socket(ep.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    await socket.ConnectAsync(ep).WaitAsync(TimeSpan.FromMilliseconds(100)).ConfigureAwait(false);
                    socket.SafeClose();
                    return (ep, string.Empty);
                }
                catch (Exception ex)
                {
                    return (ep, ex.Message);
                }
            }
        }


        int testing = 0;
        public async Task TestListen()
        {
            if (Interlocked.CompareExchange(ref testing, 1, 0) == 1)
            {
                return;
            }
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)SignInMessengerIds.Exists,
                Timeout = 2000
            });
            Interlocked.CompareExchange(ref testing, 0, 1);
            if (resp.Code == MessageResponeCodes.OK && resp.Data.Length > 0)
            {
                List<string> machineIds = MemoryPackSerializer.Deserialize<List<string>>(resp.Data.Span);
                foreach (ForwardInfo forward in running.Data.Forwards.Where(c => machineIds.Contains(c.MachineId) == false))
                {
                    running.Data.Forwards.Remove(forward);
                    Stop(forward);
                }
            }
            Version.Add();
        }

        public Dictionary<string, List<ForwardInfo>> Get()
        {
            return running.Data.Forwards.GroupBy(c => c.MachineId).ToDictionary((a) => a.Key, (b) => b.ToList());
        }
        public bool Add(ForwardInfo forwardInfo)
        {
            //同名或者同端口，但是ID不一样
            ForwardInfo old = running.Data.Forwards.FirstOrDefault(c => (c.Port == forwardInfo.Port && c.Port != 0) && c.MachineId == forwardInfo.MachineId);
            if (old != null && old.Id != forwardInfo.Id) return false;

            if (forwardInfo.Id != 0)
            {
                old = running.Data.Forwards.FirstOrDefault(c => c.Id == forwardInfo.Id);
                if (old == null) return false;

                old.BindIPAddress = forwardInfo.BindIPAddress;
                old.Port = forwardInfo.Port;
                old.Name = forwardInfo.Name;
                old.TargetEP = forwardInfo.TargetEP;
                old.MachineId = forwardInfo.MachineId;
                old.Started = forwardInfo.Started;
                old.BufferSize = forwardInfo.BufferSize;
            }
            else
            {
                forwardInfo.Id = ns.Increment();
                running.Data.Forwards.Add(forwardInfo);
            }
            running.Data.Update();

            Start();

            return true;
        }
        public bool Remove(uint id)
        {
            //同名或者同端口，但是ID不一样
            ForwardInfo old = running.Data.Forwards.FirstOrDefault(c => c.Id == id);
            if (old == null) return false;

            old.Started = false;

            running.Data.Forwards.Remove(old);
            running.Data.Update();

            Start();

            return true;
        }
    }
}
