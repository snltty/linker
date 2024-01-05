using cmonitor.libs;
using cmonitor.service;
using cmonitor.service.messengers.screen;
using cmonitor.service.messengers.setting;
using cmonitor.service.messengers.sign;
using common.libs;
using MemoryPack;
using System.Collections.Concurrent;

namespace cmonitor.client.reports.screen
{
    public sealed class ScreenShare
    {
        private readonly Config config;
        private readonly SignCaching signCaching;
        private readonly MessengerSender messengerSender;

        private readonly ConcurrentDictionary<string, string[]> shareMap = new ConcurrentDictionary<string, string[]>();

        public ScreenShare(Config config, SignCaching signCaching, MessengerSender messengerSender)
        {
            this.config = config;
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
            shareMemory.InitLocal();
        }
        public async Task Start(string machineName, string[] names)
        {
            if (config.IsCLient)
            {
                _ = Task.Run(() =>
                {
                    CommandHelper.Windows(string.Empty, new string[] {
                        $"start cmonitor.share.win.exe {config.ShareMemoryKey}/screen {2 * 1024 * 1024}"
                    });
                });
            }

            if (names.Length > 0)
            {
                shareMap.AddOrUpdate(machineName, names, (a, b) => names);

                if (signCaching.Get(machineName, out SignCacheInfo sign) && sign.Connected)
                {
                    await messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = sign.Connection,
                        MessengerId = (ushort)SettingMessengerIds.Share,
                        Payload = MemoryPackSerializer.Serialize(new SettingShareInfo { ScreenDelay = 30, ScreenScale = 1f })
                    });
                }
                foreach (string name in names)
                {
                    if (signCaching.Get(name, out sign) && sign.Connected)
                    {
                        await messengerSender.SendOnly(new MessageRequestWrap
                        {
                            Connection = sign.Connection,
                            MessengerId = (ushort)ScreenMessengerIds.ShareStart
                        });
                    }
                }
            }
        }
        public async Task Close(string machineName)
        {
            if (config.IsCLient)
            {
                shareMemory.AddAttribute(0, ShareMemoryAttribute.Closed);
            }

            if (string.IsNullOrWhiteSpace(machineName))
            {
                return;
            }
            if (shareMap.TryRemove(machineName, out string[] names))
            {
                if (signCaching.Get(machineName, out SignCacheInfo sign) && sign.Connected)
                {
                    await messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = sign.Connection,
                        MessengerId = (ushort)SettingMessengerIds.Share,
                        Payload = MemoryPackSerializer.Serialize(new SettingShareInfo { ScreenDelay = config.ScreenDelay, ScreenScale = config.ScreenScale })
                    });
                }

                foreach (string name in names)
                {
                    if (signCaching.Get(name, out sign) && sign.Connected)
                    {
                        await messengerSender.SendOnly(new MessageRequestWrap
                        {
                            Connection = sign.Connection,
                            MessengerId = (ushort)ScreenMessengerIds.ShareClose
                        });
                    }
                }
            }
        }

        public string[] GetHostNames()
        {
            return shareMap.Keys.ToArray();
        }

        public void SetData(Memory<byte> data)
        {
            shareMemory.Update(0, Helper.EmptyArray, data.Span);
            if (shareMemory.ReadAttributeEqual(0, ShareMemoryAttribute.Running) == false)
            {

            }
        }
        public async ValueTask<bool> ShareData(string machineName, Memory<byte> data)
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
                            MessengerId = (ushort)ScreenMessengerIds.ShareData,
                            Payload = data,
                        });
                    }
                }

                return true;
            }

            return false;
        }
    }

}
