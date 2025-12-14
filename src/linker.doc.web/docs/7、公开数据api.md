---
sidebar_position: 7
slug: /webapi
---

# 7、公开数据api

服务端默认监听1803端口，对外提供一些公开数据接口

公共服务器公开数据接口(使用了反向代理) https://api.linker.snltty.com

:::tip[1、在线设备数]

/flow/online.json
 
```
{
    //当前服务器
    "CurrentServer": {
        "Online7day": 499, //七天内上线
        "Online": 197 //当前在线
    },
    //所有服务器
    "AllServer": {
        "Online7day": 1468, //七天内上线
        "Online": 644, //当前在线
        "Server": 285 //服务器数
    }
}
```
:::

:::tip[2、设备区域信息]

/flow/citys.json

```
[
    {
        "City": "Beijing",  //城市
        "Lat": 39.938884,   //纬度
        "Lon": 116.397459,  //经度
        "Count": 56         //设备数
    }
]
```
:::

:::tip[3、中继节点]

/relay/nodes.json

```
[
  {
    
    "AllowProtocol": 3,             //允许的协议，1TCP 2UDP 3TCP+UDP
    "Name": "天龙上人-湖北-主服务器",  //节点名称
    "Version": "v1.9.1",            //节点版本
    "BandwidthMaxMbps": 300,        //最大带宽
    "BandwidthConnMbps": 3,         //连接带宽
    "BandwidthCurrentMbps": 0.03,   //当前带宽负载
    "BandwidthGbMonth": 100,        //每月流量
    "BandwidthByteAvailable": 12345,//可用流量字节
    "ConnectionMaxNum": 1000,       //最大连接数
    "ConnectionCurrentNum": 6,      //当前连接数
    "EndPoint": "0.0.0.0:0",        //节点地址
    "Url": "https://linker-doc.snltty.com" //一个url
  }
]
```
:::

