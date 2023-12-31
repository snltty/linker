using cmonitor.libs;
using cmonitor.service;
using cmonitor.service.messengers.screen;
using cmonitor.service.messengers.sign;
using MemoryPack;
using System.Collections.Concurrent;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace cmonitor.client.reports.screen
{
    public sealed class ScreenShare
    {
        private readonly Config config;
        private readonly ClientConfig clientConfig;
        private readonly SignCaching signCaching;
        private readonly MessengerSender messengerSender;

        private readonly ConcurrentDictionary<string, string[]> shareMap = new ConcurrentDictionary<string, string[]>();

        public ScreenShare(Config config, ClientConfig clientConfig, SignCaching signCaching, MessengerSender messengerSender)
        {
            this.config = config;
            this.clientConfig = clientConfig;
            this.signCaching = signCaching;
            this.messengerSender = messengerSender;

            if (config.IsCLient)
            {
                Init();
            }
        }


        private ShareMemory shareMemory;
        private void Init()
        {
            shareMemory = new ShareMemory($"{config.ShareMemoryKey}/screen", 1, 2 * 1024 * 1024);
            shareMemory.InitGlobal();
        }
        public async Task SetState(string machineName, ScreenShareSetupInfo screenShareSetupInfo)
        {
            if (config.IsCLient)
            {
                clientConfig.ScreenShareState = screenShareSetupInfo.State;
            }
            else
            {
                if (screenShareSetupInfo.MachineNames.Length > 0)
                {
                    shareMap.AddOrUpdate(machineName, screenShareSetupInfo.MachineNames, (a, b) => screenShareSetupInfo.MachineNames);
                    byte[] bytes = MemoryPackSerializer.Serialize(new ScreenShareSetupInfo { State = ScreenShareStates.Receiver });
                    foreach (string name in screenShareSetupInfo.MachineNames)
                    {
                        if (signCaching.Get(name, out SignCacheInfo sign) && sign.Connected)
                        {
                            await messengerSender.SendOnly(new MessageRequestWrap
                            {
                                Connection = sign.Connection,
                                MessengerId = (ushort)ScreenMessengerIds.ScreenShareState,
                                Payload = bytes,
                            });
                        }
                    }
                }
            }
        }
        public void SetData(Memory<byte> data)
        {
            shareMemory.Update(0, data.Span);
        }
        public async Task<bool> SendData(string machineName, Memory<byte> data)
        {
            if (shareMap.TryGetValue(machineName, out string[] names))
            {
                foreach (string name in names)
                {
                    if (signCaching.Get(name, out SignCacheInfo sign) && sign.Connected)
                    {
                        await messengerSender.SendOnly(new MessageRequestWrap
                        {
                            Connection = sign.Connection,
                            MessengerId = (ushort)ScreenMessengerIds.ScreenShare,
                            Payload = data,
                        });
                    }
                }

                return true;
            }

            return false;
        }
    }

    public enum ScreenShareStates : byte
    {
        None = 0,
        Sender = 1,
        Receiver = 2
    }

    [MemoryPackable]
    public sealed partial class ScreenShareSetupInfo
    {
        public ScreenShareStates State { get; set; }
        public string[] MachineNames { get; set; }
    }
}
