using cmonitor.client;
using cmonitor.client.config;
using cmonitor.plugins.forward.proxy;
using common.libs;

namespace cmonitor.plugins.forward
{
    public sealed class ForwardTransfer
    {
        private readonly RunningConfig running;
        private readonly ForwardProxy forwardProxy;

        private readonly NumberSpaceUInt32 ns = new NumberSpaceUInt32();

        public ForwardTransfer(RunningConfig running, ForwardProxy forwardProxy, ClientSignInState clientSignInState, ClientSignInTransfer clientSignInTransfer)
        {
            this.running = running;
            this.forwardProxy = forwardProxy;

            clientSignInState.NetworkFirstEnabledHandle += () =>
            {
                Start();
            };
        }

        private void Start()
        {
            uint maxid = running.Data.Forwards.Count > 0 ? running.Data.Forwards.Max(c => c.Id) : 1;
            ns.Reset(maxid);

            foreach (var item in running.Data.Forwards)
            {
                if (item.Started)
                {
                    if (item.Proxy == null)
                    {
                        try
                        {
                            item.Proxy = forwardProxy;
                            item.Proxy.Start(item.Port, item.TargetEP, item.MachineId);
                            item.Port = item.Proxy.LocalEndpoint.Port;

                            if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                                Logger.Instance.Debug($"start forward {item.Port}->{item.MachineId}->{item.TargetEP}");
                        }
                        catch (Exception ex)
                        {
                            item.Started = false;
                            Logger.Instance.Error(ex);
                        }
                    }
                }
                else
                {
                    try
                    {
                        if (item.Proxy != null)
                        {
                            if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                                Logger.Instance.Debug($"stop forward {item.Port}->{item.MachineId}->{item.TargetEP}");
                            item.Proxy.Stop(item.Port);
                            item.Proxy = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Error(ex);
                    }
                }
            }
        }

        public Dictionary<string, List<ForwardInfo>> Get()
        {
            return running.Data.Forwards.GroupBy(c => c.MachineId).ToDictionary((a) => a.Key, (b) => b.ToList());
        }
        public bool Add(ForwardInfo forwardInfo)
        {
            //同名或者同端口，但是ID不一样
            ForwardInfo old = running.Data.Forwards.FirstOrDefault(c => (c.Port == forwardInfo.Port || c.Name == forwardInfo.Name) && c.MachineId == forwardInfo.MachineId);
            if (old != null && old.Id != forwardInfo.Id) return false;

            if (forwardInfo.Id != 0)
            {
                old = running.Data.Forwards.FirstOrDefault(c => c.Id == forwardInfo.Id);
                if (old == null) return false;

                old.Port = forwardInfo.Port;
                old.Name = forwardInfo.Name;
                old.TargetEP = forwardInfo.TargetEP;
                old.MachineId = forwardInfo.MachineId;
                old.Started = forwardInfo.Started;
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
            Start();
            running.Data.Forwards.Remove(old);
            running.Data.Update();

            return true;
        }
    }
}
