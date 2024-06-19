
<!--
 * @Author: snltty
 * @Date: 2021-08-22 14:09:03
 * @LastEditors: snltty
 * @LastEditTime: 2022-11-21 16:36:26
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.webd:\desktop\cmonitor\README.md
-->
<div align="center">
<p><img src="./readme/logo.png" height="150"></p> 

# cmonitor
#### Visual Studio 2022 LTSC 17.8
<a href="https://jq.qq.com/?_wv=1027&k=ucoIVfz4" target="_blank">QQ 群：1121552990</a>

![GitHub Repo stars](https://img.shields.io/github/stars/snltty/cmonitor?style=social)
![GitHub Repo forks](https://img.shields.io/github/forks/snltty/cmonitor?style=social)
[![star](https://gitee.com/snltty/cmonitor/badge/star.svg?theme=dark)](https://gitee.com/snltty/cmonitor/stargazers)
[![fork](https://gitee.com/snltty/cmonitor/badge/fork.svg?theme=dark)](https://gitee.com/snltty/cmonitor/members)

两个模式：**监控**、**组网**

</div>

## 简单说明
1. 组件式
2. SSL加密
3. 使用了一些第三方库 **MemoryPack** 、**tun2socks** 、 **msquic**、 **SharpDX**、 **NAudio**
4. 使用了winform、aardio 做窗体

## 公共功能
##### 监控、内网穿透，都包含以下功能

- [x] 打洞连接，客户端之间打洞连接，TCP打洞、MsQuic打洞 **tunnel**
    
    1. 默认msquic.dll win11+ <a target="_blank" href="https://github.com/dotnet/runtime/tree/main/src/libraries/System.Net.Quic">官方库说明</a>，win10 请删除 msquic.dll，将 msquic-openssl.dll 更名为 msquic.dll
    2. linux 请按官方说明安装msquic

- [x] 中继连接，客户端之间通过服务器转发连接 **relay**

## 内网穿透
##### 仅内网穿透发布下，或者全功能发布下，包含以下功能
- [x] 虚拟组网，使用虚拟网卡，将各个客户端组建为局域网络 **tuntap**，使用 **tun2socks**
- [x] 端口转发，将客户端的端口转发到其它客户端的端口 **forward**
- [x] 服务器穿透，在服务器注册端口或域名，通过访问服务器端口或域名，访问内网服务 **sforward**

<img src="./readme/sforward.jpg" width="100%" />

## 监控功能
##### 仅监控发布下，或者全功能发布下，包含以下功能

- [x] 桌面捕获，捕获鼠标，**screen**，使用 **SharpDX**
- [x] 功能禁用，禁用各种系统功能 **system**
- [x] 音量控制，音量和静音 **volume**，使用 **NAudio**
- [x] 系统亮度，暂不支持外界显示器 **light**
- [x] 模拟键盘，键盘操作，模拟ctrl+alt+delete，模拟win+l，等等 **keyboard**
- [x] 发送命令，执行cmd命令，等等 **command**
- [x] 程序限制，分为禁止打开程序，和自定检测关闭程序 **active**
- [x] 网络限制，程序，域名，IP 黑白名单 **hijack**
- [x] 自动连接，wifi **wlan**
- [x] 消息提醒，向设备发送消息提醒 **message**
- [x] 全局广播，向所有设备发送广播 **notify**
- [x] 互动答题，**snatch**
- [x] 屏幕共享，以某一设备为主机，向其它设备共享屏幕，用于演示 **viewer**，使用 **RdpSession+RdpViewer**
- [x] 壁纸程序，为所有设备设置统一壁纸，以程序的方式 **wallpaper**
- [x] 锁屏程序，打开锁屏程序，禁用键盘 **llock**


## 配置文件

> 1. 修改common.json，确定要运行什么模式
> 2. 运行程序，在配置文件目录下会生成 client.json  server.json
> 3. 关闭程序，修改对应配置文件，再次运行程序

##### 1、 公共配置 common.json
```
{
  //运行在哪个模式下，多个模式可同时存在
  "Modes": ["client","server"]
}
```
##### 2、 客户端配置 client.json
```
客户端配置可以在 web 中配置，运行模式存在client时，可以浏览器打开 http://127.0.0.1:1804 进行初始化配置
```

##### 3、 服务端配置 server.json
```
{
  //中继加密秘钥，当客户端与服务端秘钥不一致时，无法使用中继
  "Relay": {
    "SecretKey": "snltty"
  },
  //监听端口
  "ServicePort": 1802,
  //服务器代理穿透配置
  "SForward": {
    //服务器代理秘钥
    "SecretKey": "snltty",
    //网页端口，可以根据域名区分不同客户端
    "WebPort": 8088,
    //隧道端口范围，根据不同端口区分不同客户端
    "TunnelPortRange": [
      10000,
      60000
    ]
  },
}
```

## 安装示例

##### 1、windows
```
使用 cmonitor.tray.win.exe
```

##### 2、linux  systemd
```
//1、下载linux版本程序，放到 /usr/local/cmonitor 文件夹，并在文件夹下创建一个 log 目录

//2、 修改文件权限
chmod 0777 cmonitor
chmoe 0777 plugins/tuntap/tun2socks

//3、写配置文件
vim /etc/systemd/system/cmonitor.service

[Unit]
Description=cmonitor

[Service]
WorkingDirectory=/usr/local/cmonitor
ExecStart=/usr/local/cmonitor/cmonitor
ExecStop=/bin/kill $MAINPID
ExecReload=/bin/kill -HUP $MAINPID
Restart=always

[Install]
WantedBy=multi-user.target


//4、重新加载配置文件
systemctl daemon-reload

//5、启动，或者重新启动
systemctl start cmonitor
systemctl restart cmonitor

//6、设置为自启动
systemctl enable cmonitor
```

##### linux docker
```
snltty/cmonitor-alpine-x64
snltty/cmonitor-alpine-arm64
```

## 发布项目

##### 1、主项目 cmonitor 项目分为三个发布配置
>1. Release 全功能
>2. ReleaseMonitor 只包含监控功能
>3. ReleaseNetwork 只包含组网功能

##### 2、发布脚本
```
publish-extends.bat   生成web和winform
publish.bat  发布主程序

public/publish 目录下查看已发布程序
```


## 支持作者

<div align="center">
请作者喝一杯咖啡，使其更有精力更新代码
<p><img src="./readme/qr.jpg" width="360"></p> 
</div>
