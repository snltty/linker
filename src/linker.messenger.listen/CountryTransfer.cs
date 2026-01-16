using linker.libs;
using linker.libs.timer;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Net;


namespace linker.messenger.listen
{
    public sealed class CountryTransfer
    {
        private string[] countryCodes = ["--","AF", "AX", "AL", "DZ", "AS", "AD", "AO", "AI", "AQ", "AG", "AR", "AM", "AW", "AU", "AT", "AZ",
            "BS", "BH", "BD", "BB", "BY", "BE", "BZ", "BJ", "BM", "BT", "BO", "BQ", "BA", "BW", "BV", "BR", "IO", "BN", "BG", "BF", "BI",
            "CV", "KH", "CM", "CA", "KY", "CF", "TD", "CL", "CN", "CX", "CC", "CO", "KM", "CG", "CD", "CK", "CR", "CI", "HR", "CU", "CW", "CY", "CZ",
            "DK", "DJ", "DM", "DO", "EC", "EG", "SV", "GQ", "ER", "EE", "SZ", "ET", "FK", "FO", "FJ", "FI", "FR",
            "GF", "PF", "TF", "GA", "GM", "GE", "DE", "GH", "GI", "GR", "GL", "GD", "GP", "GU", "GT", "GG", "GN", "GW", "GY",
            "HT", "HM", "VA", "HN", "HK", "HU", "IS", "IN", "ID", "IR", "IQ", "IE", "IM", "IL", "IT",
            "JM", "JP", "JE", "JO", "KZ", "KE", "KI", "KP", "KR", "KW", "KG",
            "LA", "LV", "LB", "LS", "LR", "LY", "LI", "LT", "LU",
            "MO", "MG", "MW", "MY", "MV", "ML", "MT", "MH", "MQ", "MR", "MU",
            "YT", "MX", "FM", "MD", "MC", "MN", "ME", "MS", "MA", "MZ", "MM", "NA", "NR", "NP", "NL", "NC", "NZ", "NI", "NE", "NG", "NU", "NF", "MK", "MP", "NO",
            "OM", "PK", "PW", "PS", "PA", "PG", "PY", "PE", "PH", "PN", "PL", "PT", "PR", "QA", "RE", "RO", "RU", "RW",
            "BL", "SH", "KN", "LC", "MF", "PM", "VC", "WS", "SM", "ST", "SA", "SN", "RS", "SC", "SL", "SG", "SX", "SK", "SI", "SB", "SO", "ZA", "GS", "SS", "ES",
            "LK", "SD", "SR", "SJ", "SE", "CH", "SY", "TW", "TJ", "TZ", "TH", "TL", "TG", "TK", "TO", "TT", "TN", "TR", "TM", "TC", "TV", "UG", "UA", "AE",
            "GB", "US", "UM", "UY", "UZ", "VU", "VE", "VN", "VG", "VI", "WF", "EH", "YE", "ZM", "ZW"];
        private readonly FrozenDictionary<string, byte> countryCodeMap;
        private readonly ConcurrentDictionary<uint, bool> ipCaches = new();

        private readonly string url = "http://ftp.apnic.net/apnic/stats/apnic/delegated-apnic-latest";
        private readonly string savePath = "delegated-apnic-latest";

        private List<CidrRecord> records = [];
        private DateTime? last;

        private readonly IListenStore store;
        public CountryTransfer(IListenStore store)
        {
            this.store = store;

            countryCodeMap = countryCodes.Select((code, index) => new { code, index }).ToDictionary(x => x.code, x => (byte)x.index).ToFrozenDictionary();

            this.url = store.GeoRegistry.Url;
            this.savePath = $"./configs/{this.url.Split('/').LastOrDefault()}";

            Worker();
        }

        public bool Test(byte type, IPAddress ip)
        {
            if (
                records.Count == 0
                || store.GeoRegistry.Messengers.Contains(type) == false
                || (store.GeoRegistry.WhiteCountry.Length == 0 && store.GeoRegistry.BlackCountry.Length == 0)
            )
            {
                return true;
            }
            if (ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            {
                return true;
            }

            Span<byte> address = stackalloc byte[4];
            ip.TryWriteBytes(address, out _);
            if (address[0] == 127 || address[0] == 10
              || (address[0] == 172 && address[1] >= 16 && address[1] <= 31)
              || (address[0] == 192 && address[1] == 168)
              || (address[0] == 169 && address[1] == 254)
              )
            {
                return true;
            }

            uint value = BinaryPrimitives.ReadUInt32BigEndian(address);
            if (ipCaches.TryGetValue(value, out bool result))
            {
                return result;
            }

            CidrRecord record = records.FirstOrDefault(c => value >= c.Start && value <= c.End);
            string country = countryCodes[record.Country];
            if (store.GeoRegistry.BlackCountry.Contains(country))
            {
                ipCaches.TryAdd(value, false);
                return false;
            }
            if (store.GeoRegistry.WhiteCountry.Length == 0 || store.GeoRegistry.WhiteCountry.Contains(country))
            {
                ipCaches.TryAdd(value, true);
                return true;
            }
            ipCaches.TryAdd(value, false);
            return false;
        }


        private void Worker()
        {
            TimerHelper.SetIntervalLong(async () =>
            {
                try
                {
                    await LoadCidr().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        LoggerHelper.Instance.Error($"CountryTransfer LoadCidr Error: {ex}");
                    }
                }
            }, 3000);
        }
        private async Task LoadCidr()
        {
            if (NeedRead())
            {
                await Read().ConfigureAwait(false);
            }
            if (await NeedUpdate().ConfigureAwait(false) == false)
            {
                return;
            }
            await Download().ConfigureAwait(false);
            await Read().ConfigureAwait(false);
        }
        private async Task Read()
        {
            using FileStream fileStream = new FileStream(savePath, FileMode.Open, FileAccess.Read);
            using StreamReader reader = new StreamReader(fileStream);
            string line = string.Empty;
            byte[] ipArray = new byte[4];
            List<CidrRecord> _records = new List<CidrRecord>();
            while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
            {
                if (string.IsNullOrWhiteSpace(line) || line[0] == '#')
                {
                    continue;
                }
                string[] parts = line.Split('|');
                if (parts.Length < 7)
                {
                    continue;
                }
                if (parts[1] == "apnic")
                {
                    last = DateTime.ParseExact(parts[2], "yyyyMMdd", null);
                    continue;
                }
                if (parts[2] != "ipv4")
                {
                    continue;
                }
                if (countryCodeMap.TryGetValue(parts[1], out byte countryByte) == false)
                {
                    continue;
                }
                if (IPAddress.TryParse(parts[3], out IPAddress ip) == false)
                {
                    continue;
                }
                ip.TryWriteBytes(ipArray, out int bytesWritten);
                uint startIp = BinaryPrimitives.ReadUInt32BigEndian(ipArray);
                _records.Add(new CidrRecord
                {
                    Country = countryByte,
                    Start = startIp,
                    End = startIp + uint.Parse(parts[4]) - 1
                });
            }
            records = _records.OrderBy(r => r.Start).ToList();
            ipCaches.Clear();
        }
        private async Task Download()
        {

            using HttpClient httpClient = new HttpClient();
            using HttpResponseMessage response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            using Stream contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            using FileStream fileStream = new FileStream(savePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            using IMemoryOwner<byte> buffer = MemoryPool<byte>.Shared.Rent(65535);
            int readBytes = 0;
            while ((readBytes = await contentStream.ReadAsync(buffer.Memory).ConfigureAwait(false)) != 0)
            {
                await fileStream.WriteAsync(buffer.Memory.Slice(0, readBytes)).ConfigureAwait(false);
            }
        }
        private async Task<bool> NeedUpdate()
        {
            return (File.Exists(savePath) == false
                || last.HasValue == false
                || (last.HasValue && last.Value.Date.ToString("yyyyMMdd") != DateTime.Now.ToString("yyyyMMdd") && DateTime.Now.Hour > 4))
                && (store.GeoRegistry.WhiteCountry.Length > 0 || store.GeoRegistry.BlackCountry.Length > 0);
        }
        private bool NeedRead()
        {
            return File.Exists(savePath) && last.HasValue == false;
        }

        struct CidrRecord
        {
            public byte Country;
            public uint Start;
            public uint End;
        }
    }
}
