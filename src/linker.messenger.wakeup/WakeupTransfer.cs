using linker.libs;
using linker.libs.extends;
using System.IO.Ports;
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
                    return SerialPort.GetPortNames();
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
                    WakeupType.Switch => await SendSwitch(info),
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
        private async Task<bool> SendSwitch(WakeupSendInfo info)
        {
            try
            {
                using SerialPort port = new SerialPort(info.Value, 9600, Parity.None, 8, StopBits.One);
                port.Open();

                byte[] openData = info.Content.Split('|')[0].Split(',').Select(c => Convert.ToByte(c, 16)).ToArray();
                byte[] closeData = info.Content.Split('|')[1].Split(',').Select(c => Convert.ToByte(c, 16)).ToArray();

                port.Write(openData, 0, openData.Length);
                await Task.Delay(info.Ms);
                port.Write(closeData, 0, closeData.Length);

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
