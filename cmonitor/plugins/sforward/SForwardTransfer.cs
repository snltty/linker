using cmonitor.client.config;
using cmonitor.client;
using common.libs;
using cmonitor.server;
using cmonitor.plugins.sforward.messenger;
using MemoryPack;
using cmonitor.plugins.sforward.config;
using System.Text;

namespace cmonitor.plugins.sforward
{
    public sealed class SForwardTransfer
    {
        private readonly RunningConfig running;
        private readonly ClientSignInState clientSignInState;
        private readonly MessengerSender messengerSender;

        private readonly NumberSpaceUInt32 ns = new NumberSpaceUInt32();

        public SForwardTransfer(RunningConfig running, ClientSignInState clientSignInState, MessengerSender messengerSender)
        {
            this.running = running;
            this.clientSignInState = clientSignInState;
            this.messengerSender = messengerSender;

            clientSignInState.NetworkFirstEnabledHandle += () =>
            {
                Start();
            };
        }

        public void SetKey()
        {

        }

        private void Start()
        {
            uint maxid = running.Data.SForwards.Count > 0 ? running.Data.SForwards.Max(c => c.Id) : 1;
            ns.Reset(maxid);

            foreach (var item in running.Data.SForwards)
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
            if (forwardInfo.Proxy == false)
            {
                try
                {
                    messengerSender.SendReply(new MessageRequestWrap
                    {
                        Connection = clientSignInState.Connection,
                        MessengerId = (ushort)SForwardMessengerIds.Add,
                        Payload = MemoryPackSerializer.Serialize(new SForwardAddInfo { Domain = forwardInfo.Domain, RemotePort = forwardInfo.RemotePort, SecretKey = running.Data.SForwardSecretKey })
                    }).ContinueWith((result) =>
                    {
                        if (result.Result.Code == MessageResponeCodes.OK)
                        {
                            SForwardAddResultInfo sForwardAddResultInfo = MemoryPackSerializer.Deserialize<SForwardAddResultInfo>(result.Result.Data.Span);
                            if (sForwardAddResultInfo.Success)
                            {
                                forwardInfo.Proxy = true;
                                Logger.Instance.Debug(sForwardAddResultInfo.Message);
                            }
                            else
                            {
                                forwardInfo.Started = false;
                                Logger.Instance.Error(sForwardAddResultInfo.Message);
                            }

                        }
                    });
                }
                catch (Exception ex)
                {
                    forwardInfo.Started = false;
                    Logger.Instance.Error(ex);
                }
            }
        }
        private void Stop(SForwardInfo forwardInfo)
        {
            try
            {
                if (forwardInfo.Proxy)
                {
                    messengerSender.SendReply(new MessageRequestWrap
                    {
                        Connection = clientSignInState.Connection,
                        MessengerId = (ushort)SForwardMessengerIds.Remove,
                        Payload = MemoryPackSerializer.Serialize(new SForwardAddInfo { Domain = forwardInfo.Domain, RemotePort = forwardInfo.RemotePort, SecretKey = running.Data.SForwardSecretKey })
                    }).ContinueWith((result) =>
                    {
                        if (result.Result.Code == MessageResponeCodes.OK)
                        {
                            SForwardAddResultInfo sForwardAddResultInfo = MemoryPackSerializer.Deserialize<SForwardAddResultInfo>(result.Result.Data.Span);
                            if (sForwardAddResultInfo.Success)
                            {
                                forwardInfo.Proxy = false;
                                Logger.Instance.Debug(sForwardAddResultInfo.Message);
                            }
                            else
                            {
                                forwardInfo.Started = true;
                                Logger.Instance.Error(sForwardAddResultInfo.Message);
                            }

                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
        }

        public List<SForwardInfo> Get()
        {
            return running.Data.SForwards;
        }
        public bool Add(SForwardInfo forwardInfo)
        {
            //同名或者同端口，但是ID不一样
            SForwardInfo old = running.Data.SForwards.FirstOrDefault(c => (forwardInfo.RemotePort > 0 && c.RemotePort == forwardInfo.RemotePort) || c.Name == forwardInfo.Name || (string.IsNullOrWhiteSpace(forwardInfo.Domain) == false && c.Domain == forwardInfo.Domain));
            if (old != null && old.Id != forwardInfo.Id) return false;

            if (forwardInfo.Id != 0)
            {
                old = running.Data.SForwards.FirstOrDefault(c => c.Id == forwardInfo.Id);
                if (old == null) return false;

                old.RemotePort = forwardInfo.RemotePort;
                old.Name = forwardInfo.Name;
                old.LocalEP = forwardInfo.LocalEP;
                old.Domain = forwardInfo.Domain;
                old.Started = forwardInfo.Started;
            }
            else
            {
                forwardInfo.Id = ns.Increment();
                running.Data.SForwards.Add(forwardInfo);
            }
            running.Data.Update();
            Start();

            return true;
        }
        public bool Remove(uint id)
        {
            //同名或者同端口，但是ID不一样
            SForwardInfo old = running.Data.SForwards.FirstOrDefault(c => c.Id == id);
            if (old == null) return false;

            old.Started = false;
            Start();
            running.Data.SForwards.Remove(old);
            running.Data.Update();

            return true;
        }
    }
}
