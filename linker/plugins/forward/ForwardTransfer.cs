using linker.client.config;
using linker.config;
using linker.libs;
using linker.plugins.client;
using linker.plugins.decenter;
using linker.plugins.forward.proxy;
using linker.plugins.messenger;
using MemoryPack;
using System.Collections.Concurrent;

namespace linker.plugins.forward
{
    public sealed class ForwardTransfer : IDecenter
    {
        public string Name => "forward";
        public VersionManager DataVersion { get; } = new VersionManager();

        private readonly FileConfig fileConfig;
        private readonly RunningConfig running;
        private readonly ForwardProxy forwardProxy;
        private readonly ClientSignInState clientSignInState;
        private readonly IMessengerSender messengerSender;
        private readonly ClientConfigTransfer clientConfigTransfer;

        private readonly NumberSpaceUInt32 ns = new NumberSpaceUInt32();
        private readonly ConcurrentDictionary<string, int> countDic = new ConcurrentDictionary<string, int>();

        public VersionManager Version { get; } = new VersionManager();

        public ForwardTransfer(FileConfig fileConfig, RunningConfig running, ForwardProxy forwardProxy, ClientSignInState clientSignInState, IMessengerSender messengerSender, ClientConfigTransfer clientConfigTransfer)
        {
            this.fileConfig = fileConfig;
            this.running = running;
            this.forwardProxy = forwardProxy;
            this.clientSignInState = clientSignInState;
            this.messengerSender = messengerSender;
            this.clientConfigTransfer = clientConfigTransfer;

            clientSignInState.NetworkEnabledHandle += Reset;
            
        }

        public Memory<byte> GetData()
        {
            CountInfo info = new CountInfo { MachineId = clientConfigTransfer.Id, Count = running.Data.Forwards.Count(c => c.GroupId == clientConfigTransfer.Group.Id) };
            countDic.AddOrUpdate(info.MachineId, info.Count, (a, b) => info.Count);
            Version.Add();
            return MemoryPackSerializer.Serialize(info);
        }
        public void SetData(Memory<byte> data)
        {
            CountInfo info = MemoryPackSerializer.Deserialize<CountInfo>(data.Span);
            countDic.AddOrUpdate(info.MachineId, info.Count, (a, b) => info.Count);
            Version.Add();
        }
        public void SetData(List<ReadOnlyMemory<byte>> data)
        {
            List<CountInfo> list = data.Select(c => MemoryPackSerializer.Deserialize<CountInfo>(c.Span)).ToList();
            foreach (var info in list)
            {
                countDic.AddOrUpdate(info.MachineId, info.Count, (a, b) => info.Count);
            }
            Version.Add();
        }
        public void RefreshConfig()
        {
            DataVersion.Add();
        }

        public ConcurrentDictionary<string, int> GetCount()
        {
            DataVersion.Add();
            return countDic;
        }

        string groupid = string.Empty;
        private void Reset(int times)
        {
            TimerHelper.Async(async () =>
            {
                if (running.Data.Forwards.All(c => string.IsNullOrWhiteSpace(c.GroupId)))
                {
                    foreach (var item in running.Data.Forwards)
                    {
                        item.GroupId = clientConfigTransfer.Group.Id;
                    }
                    running.Data.Update();
                }

                if (groupid != clientConfigTransfer.Group.Id)
                {
                    countDic.Clear();
                    Stop();
                }
                groupid = clientConfigTransfer.Group.Id;

                await Task.Delay(5000).ConfigureAwait(false);
                Start(false);
            });
        }
      
        private void Start(bool errorStop = true)
        {
            lock (this)
            {
                uint maxid = running.Data.Forwards.Count > 0 ? running.Data.Forwards.Max(c => c.Id) : 1;
                ns.Reset(maxid);

                foreach (var item in running.Data.Forwards.Where(c => c.GroupId == clientConfigTransfer.Group.Id))
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
                DataVersion.Add();
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

            Version.Add();
        }

        private void Stop()
        {
            lock (this)
            {
                foreach (var item in running.Data.Forwards)
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

        public List<ForwardInfo> Get()
        {
            return running.Data.Forwards.Where(c => c.GroupId == clientConfigTransfer.Group.Id).ToList();
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
                old.MachineName = forwardInfo.MachineName;
                old.Started = forwardInfo.Started;
                old.BufferSize = forwardInfo.BufferSize;
                old.GroupId = clientConfigTransfer.Group.Id;
            }
            else
            {
                forwardInfo.Id = ns.Increment();
                forwardInfo.GroupId = clientConfigTransfer.Group.Id;
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

    [MemoryPackable]
    public sealed partial class CountInfo
    {
        public string MachineId { get; set; }
        public int Count { get; set; }
    }
}
