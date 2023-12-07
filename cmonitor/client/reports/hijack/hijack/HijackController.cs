using cmonitor.client.reports.hijack;
using common.libs;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace cmonitor.client.reports.hijack.hijack;

public sealed class HijackController
{
    private static readonly string SystemDriver = $"{Environment.SystemDirectory}\\drivers\\netfilter2.sys";
    public const string NFDriver = "nfdriver.sys";
    public const string Name = "netfilter2";
    private readonly HijackConfig hijackConfig;
    private readonly HijackEventHandler hijackEventHandler;

    public HijackController(HijackConfig hijackConfig, HijackEventHandler hijackEventHandler)
    {
        this.hijackConfig = hijackConfig;
        this.hijackEventHandler = hijackEventHandler;

        AppDomain.CurrentDomain.ProcessExit += (sender, e) => Stop();
        Console.CancelKeyPress += (sender, e) => Stop();
    }

    public bool Start()
    {
        Stop();

        //检查安装驱动
        CheckDriver();
        //给驱动获取进程权限
        NFAPI.nf_adjustProcessPriviledges();

        //初始化驱动
        NF_STATUS nF_STATUS = NFAPI.nf_init(Name, hijackEventHandler);
        if (nF_STATUS != NF_STATUS.NF_STATUS_SUCCESS)
        {
            throw new Exception($"{Name} start failed.{nF_STATUS}");
        }
        SetRules();

        return true;
    }
    public void Stop()
    {
        try
        {
            NFAPI.nf_deleteRules();
            NFAPI.nf_free();
        }
        catch (Exception)
        {
        }
    }

    public void SetRules()
    {
        List<NF_RULE> rules = new List<NF_RULE>();

        Filter53(rules);
        FilterIPV6Lan(rules);
        FilterIPV4Lan(rules);
        FilterConfigIPs(rules);
        FilterWan(rules);

        NFAPI.nf_setRules(rules.ToArray());
    }

    private void Filter53(List<NF_RULE> rules)
    {
        rules.AddRange(new NF_RULE[] {
            //TCP 53
            new NF_RULE
            {
                direction = (byte)NF_DIRECTION.NF_D_OUT,
                filteringFlag = (uint)NF_FILTERING_FLAG.NF_INDICATE_CONNECT_REQUESTS,
                protocol = (int)ProtocolType.Tcp,
                remotePort = BinaryPrimitives.ReverseEndianness((ushort)53),
                ip_family = (ushort)AddressFamily.InterNetwork
            },
            new NF_RULE
            {
                direction = (byte)NF_DIRECTION.NF_D_OUT,
                filteringFlag = (uint)NF_FILTERING_FLAG.NF_INDICATE_CONNECT_REQUESTS,
                protocol = (int)ProtocolType.Tcp,
                remotePort = BinaryPrimitives.ReverseEndianness((ushort)53),
                ip_family = (ushort)AddressFamily.InterNetworkV6
            },
             //UDP 53
            new NF_RULE
            {
                direction = (byte)NF_DIRECTION.NF_D_OUT,
                filteringFlag = (uint)NF_FILTERING_FLAG.NF_FILTER,
                protocol = (int)ProtocolType.Udp,
                remotePort = BinaryPrimitives.ReverseEndianness((ushort)53),
                ip_family = (ushort)AddressFamily.InterNetwork
            },
            new NF_RULE
            {
                direction = (byte)NF_DIRECTION.NF_D_OUT,
                filteringFlag = (uint)NF_FILTERING_FLAG.NF_FILTER,
                protocol = (int)ProtocolType.Udp,
                remotePort = BinaryPrimitives.ReverseEndianness((ushort)53),
                ip_family = (ushort)AddressFamily.InterNetworkV6
            }
        });
    }
    private void FilterIPV6Lan(List<NF_RULE> rules)
    {
        rules.AddRange(new NF_RULE[]
        {
            //IPV6 环回 ::1/128
            new NF_RULE
            {
                direction = (byte)NF_DIRECTION.NF_D_OUT,
                filteringFlag = (uint)NF_FILTERING_FLAG.NF_ALLOW,
                ip_family = (ushort)AddressFamily.InterNetworkV6,
                remoteIpAddress = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
                remoteIpAddressMask = new byte[] { 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255 },
            },
            //IPV6 组播 FF00::/8
            new NF_RULE
            {
                direction = (byte)NF_DIRECTION.NF_D_OUT,
                filteringFlag = (uint)NF_FILTERING_FLAG.NF_ALLOW,
                ip_family = (ushort)AddressFamily.InterNetworkV6,
                remoteIpAddress = new byte[] { 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                remoteIpAddressMask = new byte[] { 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            },
            //本地链路 FE80::/10
            new NF_RULE
            {
                direction = (byte)NF_DIRECTION.NF_D_OUT,
                filteringFlag = (uint)NF_FILTERING_FLAG.NF_ALLOW,
                ip_family = (ushort)AddressFamily.InterNetworkV6,
                remoteIpAddress = new byte[] { 0xFE, 0x80, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                remoteIpAddressMask = new byte[] { 255, 192, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            },
            //本地站点 FEC0::/10
            new NF_RULE
            {
                direction = (byte)NF_DIRECTION.NF_D_OUT,
                filteringFlag = (uint)NF_FILTERING_FLAG.NF_ALLOW,
                ip_family = (ushort)AddressFamily.InterNetworkV6,
                remoteIpAddress = new byte[] { 0xFE, 0xC0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                remoteIpAddressMask = new byte[] { 255, 192, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            }
        });
    }
    private void FilterIPV4Lan(List<NF_RULE> rules)
    {
        List<string> intranetIpv4s = new List<string>() {
            "10.0.0.0/8", "100.64.0.0/10",
            "127.0.0.0/8", "169.254.0.0/16", "172.16.0.0/12",
            "192.0.0.0/24", "192.0.2.0/24","192.88.99.0/24","192.168.0.0/16",
            "198.18.0.0/15","198.51.100.0/24",
            "203.0.113.0/24","224.0.0.0/4", "240.0.0.0/4","255.255.255.255/32"
        };

        foreach (string item in intranetIpv4s)
        {
            string[] arr = item.Split('/');
            rules.Add(new NF_RULE
            {
                filteringFlag = (uint)NF_FILTERING_FLAG.NF_ALLOW,
                ip_family = (ushort)AddressFamily.InterNetwork,
                remoteIpAddress = IPAddress.Parse(arr[0]).GetAddressBytes(),
                remoteIpAddressMask = BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(0xffffffff << 32 - byte.Parse(arr[1]))),
            });
        }
    }

    private void FilterConfigIPs(List<NF_RULE> rules)
    {
        foreach (string item in hijackConfig.DeniedIPs)
        {
            string[] arr = item.Split('/');
            rules.Add(new NF_RULE
            {
                filteringFlag = (uint)NF_FILTERING_FLAG.NF_BLOCK,
                ip_family = (ushort)AddressFamily.InterNetwork,
                remoteIpAddress = IPAddress.Parse(arr[0]).GetAddressBytes(),
                remoteIpAddressMask = BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(0xffffffff << 32 - byte.Parse(arr[1]))),
            });
        }
        foreach (string item in hijackConfig.AllowIPs)
        {
            string[] arr = item.Split('/');
            rules.Add(new NF_RULE
            {
                filteringFlag = (uint)NF_FILTERING_FLAG.NF_ALLOW,
                ip_family = (ushort)AddressFamily.InterNetwork,
                remoteIpAddress = IPAddress.Parse(arr[0]).GetAddressBytes(),
                remoteIpAddressMask = BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(0xffffffff << 32 - byte.Parse(arr[1]))),
            });
        }
    }

    private void FilterWan(List<NF_RULE> rules)
    {
        rules.AddRange(new List<NF_RULE> { 
            //TCP
            new NF_RULE
            {
                direction = (byte)NF_DIRECTION.NF_D_OUT,
                filteringFlag = (uint)NF_FILTERING_FLAG.NF_FILTER,
                protocol = (int)ProtocolType.Tcp,
                ip_family = (ushort)AddressFamily.InterNetwork
            },
            new NF_RULE
            {
                direction = (byte)NF_DIRECTION.NF_D_OUT,
                filteringFlag = (uint)NF_FILTERING_FLAG.NF_FILTER,
                protocol = (int)ProtocolType.Tcp,
                ip_family = (ushort)AddressFamily.InterNetworkV6
            },
            //UDP
             new NF_RULE
            {
                direction = (byte)NF_DIRECTION.NF_D_OUT,
                filteringFlag = (uint)NF_FILTERING_FLAG.NF_FILTER,
                protocol = (int)ProtocolType.Udp,
                ip_family = (ushort)AddressFamily.InterNetwork
            },
            new NF_RULE
            {
                direction = (byte)NF_DIRECTION.NF_D_OUT,
                filteringFlag = (uint)NF_FILTERING_FLAG.NF_FILTER,
                protocol = (int)ProtocolType.Udp,
                ip_family = (ushort)AddressFamily.InterNetworkV6
            },

        });
    }


    private string GetFileVersion(string file)
    {
        if (File.Exists(file))
            return FileVersionInfo.GetVersionInfo(file).FileVersion ?? "";

        return "";
    }
    private void CheckDriver()
    {
        var binFileVersion = GetFileVersion(NFDriver);
        var systemFileVersion = GetFileVersion(SystemDriver);

        if (File.Exists(SystemDriver) == false)
        {
            // Install
            InstallDriver();
            return;
        }

        var reinstall = false;
        if (Version.TryParse(binFileVersion, out var binResult) && Version.TryParse(systemFileVersion, out var systemResult))
        {
            if (binResult.CompareTo(systemResult) > 0)
                // Update
                reinstall = true;
            else if (systemResult.Major != binResult.Major)
                // Downgrade when Major version different (may have breaking changes)
                reinstall = true;
        }
        else
        {
            // Parse File versionName to Version failed
            if (!systemFileVersion.Equals(binFileVersion))
                // versionNames are different, Reinstall
                reinstall = true;
        }

        if (!reinstall)
            return;

        UninstallDriver();
        InstallDriver();
    }
    private void InstallDriver()
    {
        if (!File.Exists(NFDriver))
            throw new Exception("builtin driver files missing, can't install NF driver");

        try
        {
            File.Copy(NFDriver, SystemDriver);
        }
        catch (Exception e)
        {
            throw new Exception($"Copy {Name}.sys failed\n{e.Message}");
        }

        // 注册驱动文件
        if (NFAPI.nf_registerDriver(Name) == NF_STATUS.NF_STATUS_SUCCESS)
        {
            Logger.Instance.Debug($"Install {Name} driver finished");
        }
        else
        {
            throw new Exception($"Register {Name} failed");
        }
    }
    private bool UninstallDriver()
    {
        Stop();

        if (File.Exists(SystemDriver) == false)
            return true;

        NFAPI.nf_unRegisterDriver(Name);
        File.Delete(SystemDriver);

        return true;
    }


}

