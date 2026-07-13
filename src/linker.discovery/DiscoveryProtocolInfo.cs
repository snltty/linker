using System.Collections.Generic;
using System.Net;
using System.Text.Json.Serialization;

namespace linker.discovery;


public sealed class DiscoveryProtocolInfo
{
    public string Name { get; set; } = string.Empty;

    public IPAddress Address { get; set; } = IPAddress.Any;

    public int Port { get; set; }

    public DiscoveryProtocolType Type { get; set; }

    public int Ttl { get; set; } = 255;

    public bool Disabled { get; set; }

    public List<IPAddress>? LanIps { get; set; } = [];

    public string Remark { get; set; } = string.Empty;

    [JsonIgnore]
    public IDiscoveryProtocolHandler? Handler { get; set; }

    internal DiscoveryProtocolInfo Clone()
    {
        return new DiscoveryProtocolInfo
        {
            Name = Name ?? string.Empty,
            Address = Address,
            Port = Port,
            Type = Type,
            Ttl = Ttl,
            Disabled = Disabled,
            LanIps = LanIps is null ? null : new List<IPAddress>(LanIps),
            Remark = Remark ?? string.Empty,
            Handler = Handler
        };
    }
}
