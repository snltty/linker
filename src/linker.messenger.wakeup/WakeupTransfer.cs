using HidSharp;
using linker.libs;
using linker.libs.extends;
using linker.messenger.decenter;
using System.Net;
using System.Net.Sockets;

namespace linker.messenger.wakeup
{
    public sealed class WakeupTransfer
    {
        public int Count => wakeupClientStore.Count();

        private readonly IWakeupClientStore wakeupClientStore;
        private readonly OperatingMultipleManager operatingMultipleManager = new OperatingMultipleManager();
        private readonly CounterDecenter counterDecenter;
        public WakeupTransfer(IWakeupClientStore wakeupClientStore, CounterDecenter counterDecenter)
        {
            this.wakeupClientStore = wakeupClientStore;
            this.counterDecenter = counterDecenter;

            counterDecenter.SetValue("wakeup", Count);
        }

        public List<WakeupInfo> Get(WakeupSearchInfo info)
        {
            List<WakeupInfo> list = wakeupClientStore.GetAll(info).ToList();

            foreach (var item in list)
            {
                item.Running = operatingMultipleManager.StringKeyValue.TryGetValue(item.Id, out bool running) && running;
            }

            return list;
        }
        public bool Add(WakeupInfo info)
        {
            wakeupClientStore.Add(info);
            counterDecenter.SetValue("wakeup", Count);
            return true;
        }
        public bool Remove(string id)
        {
            wakeupClientStore.Remove(id);
            counterDecenter.SetValue("wakeup", Count);
            return true;
        }

        public string[] ComNames()
        {
            if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                try
                {
                    return HidSharp.DeviceList.Local.GetSerialDevices().Select(c => c.DevicePath).ToArray();
                }
                catch (Exception)
                {
                }
            }
            return [];
        }
        public string[] HidIds()
        {
            if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                try
                {
                    return HidSharp.DeviceList.Local.GetHidDevices().Select(c => c.DevicePath).ToArray();
                }
                catch (Exception)
                {
                }
            }
            return [];
        }

        public async Task<bool> Send(WakeupSendInfo info)
        {
            if (operatingMultipleManager.StartOperation(info.Id) == false)
            {
                return false;
            }

            try
            {
                return info.Type switch
                {
                    WakeupType.Wol => SendWol(info),
                    WakeupType.Com => await SendCom(info),
                    WakeupType.Hid => await SendHid(info),
                    _ => false
                };
            }
            catch (Exception)
            {
            }
            finally
            {
                operatingMultipleManager.StopOperation(info.Id);
            }
            return false;
        }
        private bool SendWol(WakeupSendInfo info)
        {
            try
            {
                //MAC地址格式：XX:XX:XX:XX:XX:XX 或者 XX-XX-XX-XX-XX-XX 或者 XXXXXXXXXXXX 转字节数组
                ReadOnlySpan<char> macAddress = info.Value.AsSpan();
                Span<byte> macBytes = stackalloc byte[6];
                int byteIndex = 0;
                for (int i = 0; i < macAddress.Length && byteIndex < 6; i++)
                {
                    char c = macAddress[i];
                    if (char.IsAsciiHexDigit(c))
                    {
                        if (i + 1 < macAddress.Length && char.IsAsciiHexDigit(macAddress[i + 1]))
                        {
                            macBytes[byteIndex++] = (byte)((HexToNibble(c) << 4) | HexToNibble(macAddress[i + 1]));
                            i++;
                        }
                    }
                }

                Span<byte> magicPacket = stackalloc byte[102];
                //6个连续的0xFF
                stackalloc byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }.CopyTo(magicPacket.Slice(0, 6));
                //16个重复的MAC地址
                for (int i = 1; i <= 16; i++)
                {
                    macBytes.CopyTo(magicPacket.Slice(i * 6, 6));
                }

                using UdpClient client = new UdpClient();
                client.EnableBroadcast = true;
                client.Client.WindowsUdpBug();
                client.Send(magicPacket, new IPEndPoint(IPAddress.Parse("255.255.255.255"), 9));
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
            }
            return false;
        }
        private static byte HexToNibble(char c)
        {
            return (byte)(char.ToUpperInvariant(c) switch
            {
                >= '0' and <= '9' => c - '0',
                >= 'A' and <= 'F' => c - 'A' + 10,
                _ => throw new FormatException($"Invalid hex character: {c}")
            });
        }

        private async Task<bool> SendCom(WakeupSendInfo info)
        {
            try
            {
                byte road = byte.Parse(info.Content);

                SerialDevice device = HidSharp.DeviceList.Local.GetSerialDevices().FirstOrDefault(c => c.DevicePath == info.Value);
                using SerialStream stream = device.Open();

                stream.Write([0xA0, road, 0x01, (byte)(0xA0 + road + 0x01)]);
                stream.Flush();
                await Task.Delay(info.Ms);
                stream.Write([0xA0, road, 0x00, (byte)(0xA0 + road + 0x00)]);
                stream.Flush();

                return true;
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
            }
            return false;

        }
        private async Task<bool> SendHid(WakeupSendInfo info)
        {
            try
            {
                byte road = byte.Parse(info.Content);
                HidDevice device = HidSharp.DeviceList.Local.GetHidDevices().FirstOrDefault(c => c.DevicePath == info.Value);
                using HidStream stream = device.Open();

                stream.Write([0x00, 0xA0, road, 0x01, (byte)(0xA0 + road + 0x01)]);
                stream.Flush();
                await Task.Delay(info.Ms);
                stream.Write([0x00, 0xA0, road, 0x00, (byte)(0xA0 + road + 0x00)]);
                stream.Flush();

                return true;
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
            }
            return false;

        }

        private void OnlineTest()
        {
            //netsh interface ip delete arpcache
            //arp -a

            //ip -s -s neigh flush all   
            //ip neigh

            //arp -d -a   
            //arp -a
        }
    }

}
