using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;

namespace linker.upnp
{
    public sealed class UpnpDevice : IPortMappingDevice
    {
        /// <summary>
        /// 设备类型
        /// </summary>
        public DeviceType Type => DeviceType.Upnp;
        public IPAddress GatewayIp { get; set; }
        public IPAddress WanIp { get; set; }

        /// <summary>
        /// 服务类型描述
        /// </summary>
        public string ServiceType { get; set; }
        /// <summary>
        /// 控制地址
        /// </summary>
        public string ControlUrl { get; set; }
        /// <summary>
        /// 内网ip
        /// </summary>
        public IPAddress ClientIp { get; set; }
        public async Task<List<PortMappingInfo>> Get()
        {
            List<PortMappingInfo> result = new List<PortMappingInfo>();
            using HttpClient httpClient = new HttpClient();
            for (int index = 0; ; index++)
            {
                string action = BuildGetPortMappingRequest(index);

                try
                {
                    using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, ControlUrl);
                    request.Headers.Add("SOAPACTION", $"\"{ServiceType}#GetGenericPortMappingEntry\"");
                    request.Content = new StringContent(action, Encoding.UTF8, "text/xml");
                    using HttpResponseMessage response = await httpClient.SendAsync(request).ConfigureAwait(false);
                    string responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    if (responseContent.Contains("UPnPError"))
                    {
                        break;
                    }

                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(responseContent);
                    XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
                    nsmgr.AddNamespace("s", "http://schemas.xmlsoap.org/soap/envelope/");
                    nsmgr.AddNamespace("u", "urn:schemas-upnp-org:service:WANIPConnection:1");
                    XmlNode resp = doc.SelectSingleNode("//u:GetGenericPortMappingEntryResponse", nsmgr);

                    result.Add(new PortMappingInfo
                    {
                        ClientIp = IPAddress.Parse(resp.SelectSingleNode("NewInternalClient").InnerText),
                        Description = resp.SelectSingleNode("NewPortMappingDescription").InnerText,
                        Enabled = resp.SelectSingleNode("NewEnabled").InnerText == "1",
                        LeaseDuration = int.Parse(resp.SelectSingleNode("NewLeaseDuration").InnerText),
                        PrivatePort = int.Parse(resp.SelectSingleNode("NewInternalPort").InnerText),
                        PublicPort = int.Parse(resp.SelectSingleNode("NewExternalPort").InnerText),
                        ProtocolType = resp.SelectSingleNode("NewProtocol").InnerText == "TCP" ? ProtocolType.Tcp : ProtocolType.Udp,
                        DeviceType = DeviceType.Upnp
                    });
                }
                catch (Exception)
                {
                }

            }
            return result;
        }
        public async Task<bool> Add(PortMappingInfo mapping)
        {
            if ((mapping.DeviceType & Type) != Type) return false;

            string action = BuildAddPortMappingRequest(mapping);

            using HttpClient httpClient = new HttpClient();
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, ControlUrl);
            request.Headers.Add("SOAPACTION", $"\"{ServiceType}#AddPortMapping\"");
            request.Content = new StringContent(action, Encoding.UTF8, "text/xml");
            using HttpResponseMessage response = await httpClient.SendAsync(request).ConfigureAwait(false);
            string responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return responseContent.Contains("AddPortMappingResponse");
        }
        public async Task<bool> Delete(int publicPort, ProtocolType protocol)
        {
            string action = BuildDeletePortMappingRequest(publicPort, protocol);
            using HttpClient httpClient = new HttpClient();
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, ControlUrl);
            request.Headers.Add("SOAPACTION", $"\"{ServiceType}#DeletePortMapping\"");
            request.Content = new StringContent(action, Encoding.UTF8, "text/xml");
            using HttpResponseMessage response = await httpClient.SendAsync(request).ConfigureAwait(false);
            string responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            return responseContent.Contains("DeletePortMappingResponse");
        }

        private string BuildAddPortMappingRequest(PortMappingInfo mapping)
        {
            if (mapping.ClientIp == null || mapping.ClientIp.Equals(IPAddress.Any))
            {
                mapping.ClientIp = ClientIp;
            }
            return $@"<?xml version=""1.0""?>
<s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"" 
            s:encodingStyle=""http://schemas.xmlsoap.org/soap/encoding/"">
  <s:Body>
    <u:AddPortMapping xmlns:u=""{ServiceType}"">
      <NewRemoteHost></NewRemoteHost>
      <NewExternalPort>{mapping.PublicPort}</NewExternalPort>
      <NewProtocol>{mapping.ProtocolType.ToString().ToUpper()}</NewProtocol>
      <NewInternalPort>{mapping.PrivatePort}</NewInternalPort>
      <NewInternalClient>{mapping.ClientIp}</NewInternalClient>
      <NewEnabled>{(mapping.Enabled ? "1" : "0")}</NewEnabled>
      <NewPortMappingDescription>{mapping.Description}</NewPortMappingDescription>
      <NewLeaseDuration>{mapping.LeaseDuration}</NewLeaseDuration>
    </u:AddPortMapping>
  </s:Body>
</s:Envelope> ";
        }
        private string BuildDeletePortMappingRequest(int publicPort, ProtocolType protocol)
        {
            return $@"<?xml version=""1.0""?>
<s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"" 
            s:encodingStyle=""http://schemas.xmlsoap.org/soap/encoding/"">
  <s:Body>
    <u:DeletePortMapping xmlns:u=""{ServiceType}"">
      <NewRemoteHost></NewRemoteHost>
      <NewExternalPort>{publicPort}</NewExternalPort>
      <NewProtocol>{protocol.ToString().ToUpper()}</NewProtocol>
    </u:DeletePortMapping>
  </s:Body>
</s:Envelope>";
        }
        private string BuildGetPortMappingRequest(int index)
        {
            return $@"<?xml version=""1.0""?>
<s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"" 
            s:encodingStyle=""http://schemas.xmlsoap.org/soap/encoding/"">
  <s:Body>
    <u:GetGenericPortMappingEntry xmlns:u=""{ServiceType}"">
      <NewPortMappingIndex>{index}</NewPortMappingIndex>
    </u:GetGenericPortMappingEntry>
  </s:Body>
</s:Envelope>";
        }

        public override string ToString()
        {
            return $"类型:UPNP->客户端ip:{ClientIp}->控制地址:{ControlUrl}->服务描述:{ServiceType}";
        }

    }
    public sealed class PortMappingUpnpService : IPortMappingService
    {
        public DeviceType Type => DeviceType.Upnp;


        private readonly ConcurrentDictionary<string, IPortMappingDevice> upnpDevices = new();
        private CancellationTokenSource cts;
        private const string discoveryMessage =
                         "M-SEARCH * HTTP/1.1\r\n" +
                         "HOST: 239.255.255.250:1900\r\n" +
                         "MAN: \"ssdp:discover\"\r\n" +
                         "MX: 3\r\n" +
                         "ST: urn:schemas-upnp-org:device:InternetGatewayDevice:1\r\n" +
                         "USER-AGENT: UPnP/1.0\r\n" +
                         "\r\n";
        private byte[] messageBytes = Encoding.ASCII.GetBytes(discoveryMessage);

        public async Task<List<IPortMappingDevice>> Discovery(CancellationToken token)
        {
            try
            {
                List<Task<(string response, IPAddress ip, IPAddress gateway)>> tasks = GetLocalIp().Select(async (ip) =>
                {
                    using UdpClient client = CreateClient(ip);
                    try
                    {

                        for (int i = 0; i < 3; i++)
                        {
                            await client.SendAsync(messageBytes, messageBytes.Length, new IPEndPoint(IPAddress.Parse("239.255.255.250"), 1900)).ConfigureAwait(false);
                        }
                        var _cts = new CancellationTokenSource(3000);
                        while (token.IsCancellationRequested == false && _cts.Token.IsCancellationRequested == false)
                        {
                            UdpReceiveResult result = await client.ReceiveAsync(_cts.Token).ConfigureAwait(false);

                            return (Encoding.ASCII.GetString(result.Buffer), ip, (result.RemoteEndPoint as IPEndPoint).Address);
                        }
                    }
                    catch (Exception)
                    {
                    }
                    client?.Dispose();
                    return (string.Empty, ip, IPAddress.Any);
                }).ToList();
                var responses = await Task.WhenAll(tasks).ConfigureAwait(false);
                foreach (var response in responses.Where(c => string.IsNullOrWhiteSpace(c.response) == false))
                {
                    UpnpDevice device = await PrarseDevice(response.response, response.ip, response.gateway).ConfigureAwait(false);
                    if (device != null)
                    {
                        if (upnpDevices.TryGetValue(device.ControlUrl, out IPortMappingDevice _device) == false)
                        {
                            _device = device;
                            upnpDevices.TryAdd(device.ControlUrl, device);
                        }
                    }
                }
                return upnpDevices.Values.ToList<IPortMappingDevice>();
            }
            catch (Exception)
            {
            }
            return [];
        }



        private async Task<UpnpDevice> PrarseDevice(string respStr, IPAddress clientIp, IPAddress gateway)
        {
            try
            {
                int start = respStr.AsSpan().IndexOf("LOCATION: ");
                int end = respStr.AsSpan().Slice(start + 10).IndexOf("\r\n");
                string location = respStr.AsSpan().Slice(start + 10, end).ToString();

                using HttpClient webClient = new HttpClient();
                string resp = await webClient.GetStringAsync(location).ConfigureAwait(false);


                XmlDocument doc = new XmlDocument();
                doc.LoadXml(resp);
                XmlNamespaceManager ns = new XmlNamespaceManager(doc.NameTable);
                ns.AddNamespace("upnp", "urn:schemas-upnp-org:device-1-0");
                var serviceNodes = doc.SelectNodes("//upnp:service", ns);
                foreach (XmlNode service in serviceNodes)
                {
                    string serviceType = service.SelectSingleNode("upnp:serviceType", ns)?.InnerText;
                    if (string.IsNullOrWhiteSpace(serviceType) || (serviceType.Contains("WANIPConnection") == false && serviceType.Contains("WANPPPConnection") == false))
                    {
                        continue;
                    }
                    string controlUrl = service.SelectSingleNode("upnp:controlURL", ns)?.InnerText;
                    if (string.IsNullOrWhiteSpace(controlUrl))
                    {
                        continue;
                    }

                    Uri baseUri = new Uri(location);
                    if (!controlUrl.StartsWith("http"))
                    {
                        controlUrl = new Uri(baseUri, controlUrl).ToString();
                    }
                    return new UpnpDevice
                    {
                        ServiceType = serviceType,
                        ControlUrl = controlUrl,
                        ClientIp = clientIp,
                        GatewayIp = gateway,
                        WanIp = gateway
                    };
                }
            }
            catch (Exception)
            {
            }
            return null;
        }
        private List<IPAddress> GetLocalIp()
        {
            return NetworkInterface.GetAllNetworkInterfaces().Where(c => c.OperationalStatus == OperationalStatus.Up)
                .SelectMany(c => c.GetIPProperties().UnicastAddresses)
                .Select(c => c.Address).Where(c => c.AddressFamily == AddressFamily.InterNetwork)
                .Where(c => c.Equals(IPAddress.Loopback) == false).ToList();
        }
        private UdpClient CreateClient(IPAddress ip)
        {
            UdpClient client = new UdpClient();
            client.Client.SetSocketOption(SocketOptionLevel.Socket,
                SocketOptionName.ReuseAddress, true);
            client.Client.Bind(new IPEndPoint(ip, 0));
            client.JoinMulticastGroup(IPAddress.Parse("239.255.255.250"));
            client.Client.SetSocketOption(SocketOptionLevel.IP,
                SocketOptionName.MulticastTimeToLive, 4);
            client.Client.SetSocketOption(SocketOptionLevel.IP,
                SocketOptionName.MulticastLoopback, true);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    const uint IOC_IN = 0x80000000;
                    int IOC_VENDOR = 0x18000000;
                    int SIO_UDP_CONNRESET = (int)(IOC_IN | IOC_VENDOR | 12);
                    client.Client.IOControl((int)SIO_UDP_CONNRESET, new byte[] { Convert.ToByte(false) }, null);
                }
                catch (Exception)
                {
                }
            }
            return client;
        }

        /// <summary>
        /// 获取所有已找到的upnp设备
        /// </summary>
        /// <returns></returns>
        public List<IPortMappingDevice> GetDevices()
        {
            return upnpDevices.Values.ToList<IPortMappingDevice>();
        }

        /// <summary>
        /// 获取所有upnp设备的所有映射信息
        /// </summary>
        /// <returns></returns>
        public async Task<List<PortMappingInfo>> Get()
        {
            return (await Task.WhenAll(upnpDevices.Values.Select(c => c.Get()).ToList()).ConfigureAwait(false)).SelectMany(c => c).ToList();
        }
        /// <summary>
        /// 添加一条映射
        /// </summary>
        /// <param name="mapping"></param>
        /// <returns></returns>
        public async Task Add(PortMappingInfo mapping)
        {
            foreach (var device in upnpDevices.Values)
            {
                await device.Add(mapping).ConfigureAwait(false);
            }
        }
        /// <summary>
        /// 删除一条映射
        /// </summary>
        /// <param name="publicPort"></param>
        /// <param name="protocol"></param>
        /// <returns></returns>
        public async Task Delete(int publicPort, ProtocolType protocol)
        {
            foreach (var device in upnpDevices.Values)
            {
                await device.Delete(publicPort, protocol).ConfigureAwait(false);
            }
        }
    }
}
