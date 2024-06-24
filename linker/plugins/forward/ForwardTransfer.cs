using linker.client;
using linker.client.config;
using linker.libs;
using linker.plugins.forward.proxy;

namespace linker.plugins.forward
{
    public sealed class ForwardTransfer
    {
        private readonly RunningConfig running;
        private readonly ForwardProxy forwardProxy;

        private readonly NumberSpaceUInt32 ns = new NumberSpaceUInt32();

        public ForwardTransfer(RunningConfig running, ForwardProxy forwardProxy, ClientSignInState clientSignInState)
        {
            this.running = running;
            this.forwardProxy = forwardProxy;

            clientSignInState.NetworkFirstEnabledHandle += Start;
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
                    forwardProxy.Start(new System.Net.IPEndPoint(forwardInfo.BindIPAddress, forwardInfo.Port), forwardInfo.TargetEP, forwardInfo.MachineId);
                    forwardInfo.Port = forwardProxy.LocalEndpoint.Port;

                    if(forwardInfo.Port > 0)
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
        }
        private void Stop(ForwardInfo forwardInfo)
        {
            try
            {
                if (forwardInfo.Proxy)
                {
                    LoggerHelper.Instance.Debug($"stop forward {forwardInfo.Port}->{forwardInfo.MachineId}->{forwardInfo.TargetEP}");
                    forwardProxy.Stop(forwardInfo.Port);
                    forwardInfo.Proxy = false;
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
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

                old.BindIPAddress = forwardInfo.BindIPAddress;
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
