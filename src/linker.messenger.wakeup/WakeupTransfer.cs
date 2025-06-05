using HidSharp;
using linker.libs;
using linker.libs.extends;
using System.Net;
using System.Net.Sockets;

namespace linker.messenger.wakeup
{
    public sealed class WakeupTransfer
    {
        private readonly IWakeupClientStore wakeupClientStore;
        private readonly OperatingMultipleManager operatingMultipleManager = new OperatingMultipleManager();
        public WakeupTransfer(IWakeupClientStore wakeupClientStore)
        {
            this.wakeupClientStore = wakeupClientStore;
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
            return true;
        }
        public bool Remove(string id)
        {
            wakeupClientStore.Remove(id);
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
                    WakeupType.Wol => await SendWol(info),
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
        private async Task<bool> SendWol(WakeupSendInfo info)
        {
            try
            {
                string macAddress = info.Value.Replace(":", "").Replace("-", "").Replace(" ", "").ToUpper();

                using UdpClient client = new UdpClient();
                client.EnableBroadcast = true;
                client.Client.WindowsUdpBug();

                byte[] macBytes = Enumerable.Range(0, macAddress.Length)
                    .Where(x => x % 2 == 0)
                    .Select(x => Convert.ToByte(macAddress.Substring(x, 2), 16))
                    .ToArray();

                byte[] magicPacket = new byte[102];
                for (int i = 0; i < 6; i++)
                {
                    magicPacket[i] = 0xFF;
                }
                for (int i = 1; i <= 16; i++)
                {
                    Array.Copy(macBytes, 0, magicPacket, i * 6, 6);
                }

                IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("255.255.255.255"), 9);
                await client.SendAsync(magicPacket, magicPacket.Length, endPoint);
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
