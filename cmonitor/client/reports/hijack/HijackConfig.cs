using common.libs.winapis;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;

namespace cmonitor.client.reports.hijack
{
    public sealed class HijackConfig
    {
        public HijackConfig()
        {

        }

        /// <summary>
        /// 进程白名单
        /// </summary>
        public string[] AllowProcesss { get; set; } = Array.Empty<string>();
        /// <summary>
        /// 进程黑名单
        /// </summary>
        public string[] DeniedProcesss { get; set; } = Array.Empty<string>();

        /// <summary>
        /// 域名白名单
        /// </summary>
        public string[] AllowDomains { get; set; } = Array.Empty<string>();
        /// <summary>
        /// 域名黑名单
        /// </summary>
        public string[] DeniedDomains { get; set; } = Array.Empty<string>();

        /// <summary>
        /// ip白名单
        /// </summary>
        public string[] AllowIPs { get; set; } = Array.Empty<string>();
        /// <summary>
        /// ip黑名单
        /// </summary>
        public string[] DeniedIPs { get; set; } = Array.Empty<string>();

        /// <summary>
        /// true 白名单  false 黑名单
        /// </summary>
        public ConcurrentDictionary<IPAddress, bool> DomainIPs { get; } = new ConcurrentDictionary<IPAddress, bool>(new IPAddressComparer());
        private bool started = false;
        private uint updateLength = 0;
        public void UpdateTask()
        {
            if (started) return;
            started = true;
            Task.Run(async () =>
            {
                while (true)
                {
                    while (updateLength > 0)
                    {
                        UpdateDomainIPs();
                        updateLength--;

                    }
                    await Task.Delay(3000);
                }
            });
            Update();
        }
        private void UpdateDomainIPs()
        {
            DomainIPs.Clear();
            foreach (string domain in AllowDomains)
            {
                IPHostEntry entry = Dns.GetHostEntry(domain);
                foreach (var item in entry.AddressList)
                {
                    DomainIPs[item] = true;
                }
            }
            foreach (string domain in DeniedDomains)
            {
                IPHostEntry entry = Dns.GetHostEntry(domain);
                foreach (IPAddress item in entry.AddressList)
                {
                    if (DomainIPs.ContainsKey(item) == false)
                        DomainIPs[item] = false;
                }
            }
            List<int> adapters = Wininet.GetAdaptersIndex();
            List<IPAddress> ips = DomainIPs.Where(c => c.Value == false).Select(c=>c.Key).ToList();
            if(ips.Any() && adapters.Any())
            {
                Wininet.DeleteConnection(adapters, ips);
            }
        }

        public void Update()
        {
            updateLength++;
        }

    }

    public sealed class IPAddressComparer : IEqualityComparer<IPAddress>
    {
        public bool Equals(IPAddress x, IPAddress y)
        {
            return x != null && y != null && x.Equals(y);
        }

        public int GetHashCode(IPAddress obj)
        {
            return obj.GetHashCode();
        }
    }

}
