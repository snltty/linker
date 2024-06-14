
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

两个发布模式：**监控**、**组网**、<a href="https://github.com/snltty/cmonitor/wiki" target="_blank">使用说明wiki</a>

</div>

## 简单说明
1. 组件式（随意拆卸）
2. 与信标服务器通信，打洞通信，均使用SSL加密
3. 使用了 **MemoryPack** 进行数据序列化反序列化
4. 程序分为两类，**monitor** 只包含监控功能，**network** 只包含内网穿透功能

## 公共功能
##### 监控、内网穿透，都包含以下功能

- [x] 打洞连接，客户端之间打洞连接，TCP打洞、MsQuic打洞 **tunnel**
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


## 支持作者

<div align="center">
请作者喝一杯咖啡，使其更有精力更新代码
<p><img src="./readme/qr.jpg" width="360"></p> 
</div>
