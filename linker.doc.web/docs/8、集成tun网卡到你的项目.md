---
sidebar_position: 8
---

# 8、集成tun网卡到你的项目

在你的.NET8.0+项目中，集成tun网卡，适用于`windows`、`linux`

## 1、windows

[下载wintun](https://www.wintun.net/)，选择适合你系统的 `wintun.dll`放到项目根目录

## 2、linux

请确保你的系统拥有`tuntap`模块，`ifconfig`、`ip`、`iptables`命令

## 3、编写一个简单的代码

nuget 安装 `linker.tun`，然后编写代码

```c#

internal class Program
{
    public static LinkerTunDeviceAdapter linkerTunDeviceAdapter;
    static void Main(string[] args)
    {
        linkerTunDeviceAdapter = new LinkerTunDeviceAdapter();
        //设置网卡IP包回调
        linkerTunDeviceAdapter.SetReadCallback(new LinkerTunDeviceCallback());
        //启动网卡
        linkerTunDeviceAdapter.SetUp(
            "linker" //网卡名称
             //windows下，使用一个固定guid，否则网卡编号会不断递增，注册表不断产生新纪录
            , Guid.Parse("dc6d4efa-2b53-41bd-a403-f416c9bf7129")
            , IPAddress.Parse("192.168.54.2"), 24); //网卡IP和掩码
        //设置MTU
        linkerTunDeviceAdapter.SetMtu(1420);

        //如果存在错误
        if (string.IsNullOrWhiteSpace(linkerTunDeviceAdapter.Error))
        {
            Console.WriteLine(linkerTunDeviceAdapter.Error);
            //关闭网卡
            linkerTunDeviceAdapter.Shutdown();
        }
        Console.ReadLine();
    }
}

public sealed class LinkerTunDeviceCallback : ILinkerTunDeviceCallback
{
    //收到IP数据包
    public async Task Callback(LinkerTunDevicPacket packet)
    {
        ICMPAnswer(packet);
        await Task.CompletedTask;
    }
    private unsafe void ICMPAnswer(LinkerTunDevicPacket packet)
    {
        //去掉首部表示包长度的4字节，
        Memory<byte> writableMemory = MemoryMarshal.AsMemory(packet.Packet.Slice(4));
        fixed (byte* ptr = writableMemory.Span)
        {
            //ICMP包，且是 Request
            if (ptr[9] == 1 && ptr[20] == 8)
            {
                Console.WriteLine($"ICMP to {new IPAddress(writableMemory.Span.Slice(16, 4))}");

                uint dist = BinaryPrimitives.ReadUInt32LittleEndian(writableMemory.Span.Slice(16, 4));
                //目的地址变源地址，
                *(uint*)(ptr + 16) = *(uint*)(ptr + 12);
                //假装是网关回复的
                *(uint*)(ptr + 12) = dist;

                //计算一次IP头校验和
                *(ushort*)(ptr + 10) = 0;
                *(ushort*)(ptr + 10) = Program.linkerTunDeviceAdapter.Checksum((ushort*)ptr, 20);

                //改为ICMP Reply
                *(ushort*)(ptr + 20) = 0;

                //计算ICMP校验和
                *(ushort*)(ptr + 22) = 0;
                *(ushort*)(ptr + 22) = Program.linkerTunDeviceAdapter.Checksum((ushort*)(ptr + 20), (uint)(writableMemory.Length - 20));

                //写入网卡，回应这个ICMP请求
                Program.linkerTunDeviceAdapter.Write(writableMemory);
            }
        }
    }
}
```