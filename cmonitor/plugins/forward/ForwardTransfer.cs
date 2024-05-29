using cmonitor.client;
using cmonitor.config;
using cmonitor.plugins.forward.proxy;
using common.libs;

namespace cmonitor.plugins.forward
{
    public sealed class ForwardTransfer
    {
        private readonly Config config;
        private readonly ForwardProxy forwardProxy;

        private readonly NumberSpaceUInt32 ns = new NumberSpaceUInt32();

        public ForwardTransfer(Config config, ForwardProxy forwardProxy, ClientSignInState clientSignInState, ClientSignInTransfer clientSignInTransfer)
        {
            this.config = config;
            this.forwardProxy = forwardProxy;

            clientSignInState.NetworkFirstEnabledHandle += () =>
            {
                Start();
            };
            clientSignInTransfer.NameChanged += (string oldName, string newName) =>
            {
                foreach (var item in config.Data.Client.Forward.Forwards.Where(c=>c.MachineName == oldName))
                {
                    item.MachineName = newName;
                }
            };
        }

        private void Start()
        {
            uint maxid = config.Data.Client.Forward.Forwards.Count > 0 ? config.Data.Client.Forward.Forwards.Max(c => c.ID) : 1;
            ns.Reset(maxid);

            foreach (var item in config.Data.Client.Forward.Forwards)
            {
                if (item.Started)
                {
                    if (item.Proxy == null)
                    {
                        try
                        {
                            item.Proxy = forwardProxy;
                            item.Proxy.Start(item.Port, item.TargetEP, item.MachineName);
                            item.Port = item.Proxy.LocalEndpoint.Port;

                            if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                                Logger.Instance.Debug($"start forward {item.Port}->{item.MachineName}->{item.TargetEP}");
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
                                Logger.Instance.Debug($"stop forward {item.Port}->{item.MachineName}->{item.TargetEP}");
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
            return config.Data.Client.Forward.Forwards.GroupBy(c => c.MachineName).ToDictionary((a) => a.Key, (b) => b.ToList());
        }
        public bool Add(ForwardInfo forwardInfo)
        {
            //同名或者同端口，但是ID不一样
            ForwardInfo old = config.Data.Client.Forward.Forwards.FirstOrDefault(c => (c.Port == forwardInfo.Port || c.Name == forwardInfo.Name) && c.MachineName == forwardInfo.MachineName);
            if (old != null && old.ID != forwardInfo.ID) return false;

            if (forwardInfo.ID != 0)
            {
                old = config.Data.Client.Forward.Forwards.FirstOrDefault(c => c.ID == forwardInfo.ID);
                if (old == null) return false;

                old.Port = forwardInfo.Port;
                old.Name = forwardInfo.Name;
                old.TargetEP = forwardInfo.TargetEP;
                old.MachineName = forwardInfo.MachineName;
                old.Started = forwardInfo.Started;
            }
            else
            {
                forwardInfo.ID = ns.Increment();
                config.Data.Client.Forward.Forwards.Add(forwardInfo);
            }
            config.Save();
            Start();

            return true;
        }
        public bool Remove(uint id)
        {
            //同名或者同端口，但是ID不一样
            ForwardInfo old = config.Data.Client.Forward.Forwards.FirstOrDefault(c => c.ID == id);
            if (old == null) return false;

            old.Started = false;
            Start();
            config.Data.Client.Forward.Forwards.Remove(old);
            config.Save();

            return true;
        }
    }
}
