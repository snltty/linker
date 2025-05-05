
<!--
 * @Author: snltty
 * @Date: 2021-08-22 14:09:03
 * @LastEditors: snltty
 * @LastEditTime: 2022-11-21 16:36:26
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.webd:\desktop\linker\README.md
-->
<div align="center">
<p><img src="./readme/logo.png" height="150"></p> 

# .NET8.0、linker、link anywhere

![GitHub Repo stars](https://img.shields.io/github/stars/snltty/linker?style=social)
![GitHub Repo forks](https://img.shields.io/github/forks/snltty/linker?style=social)
[![star](https://gitee.com/snltty/linker/badge/star.svg?theme=dark)](https://gitee.com/snltty/linker/stargazers)
[![fork](https://gitee.com/snltty/linker/badge/fork.svg?theme=dark)](https://gitee.com/snltty/linker/members)

<a href="https://linker.snltty.com">官方网站</a>、<a href="https://linker-doc.snltty.com">使用说明文档</a>、<a href="https://jq.qq.com/?_wv=1027&k=ucoIVfz4" target="_blank">加入组织：1121552990</a>

[README](README.md) | [中文说明](README_zh.md)

**使用人员有责任和义务遵守当地法律条规，请勿用于违法犯罪**

</div>

## 大概意思

使用p2p或者中继转发，让你的各个局域网连通起来，让各个局域网内的任意联网设备都可以相互连通

<div align="center">
<p><img src="./readme/linker.jpg"></p> 
</div>

## 支持平台

|  | amd64 | x86 | arm64 | arm | 
|-------|-------|-------|-------|-------|
| Windows | ✔ | ✔ |✔ | |
| Linux | ✔ |  |✔ |✔ |
| Linux Musl | ✔ |  |✔ |✔ |
| Openwrt | ✔ |  |✔ |✔ |
| Android | ✔ |  |  | |


## 主要功能

##### 打洞中继
- [x] 打洞连接，支持`TCP、UDP、IPV4、IPV6`
- [x] 中继连接，自建中继节点，支持`多中继节点`

##### 通信方式
- [x] 异地组网，`点对点`、`点对网`、`网对网`、`自动分配虚拟IP`、`网段映射`(处理多局域网网段冲突)
- [x] 端口转发，将客户端的端口转发到其它客户端的端口
- [x] 服务器穿透，使用端口或域名访问内网服务(支持`计划任务`，定时定长自动开启关闭)
- [x] socks5代理，端口转发需要指定端口，而socks5代理可以代理所有端口

##### 其它功能
- [x] 配置文件加密
- [x] 权限管理，主客户端拥有完全权限，可导出、配置子客户端配置，分配其管理权限
- [x] 自定义验证，通过`HTTP POST`让你可以自定义认证是否允许`连接信标`，`中继`，`内网穿透`
- [x] 流量统计，统计服务器`信标`、`中继`、`内网穿透` 的流量情况
- [x] CDKEY，可以临时解锁一些限制，中继，内外穿透什么的
- [x] 更多功能，<a href="https://linker-doc.snltty.com">使用说明文档</a>

## 二开集成
- [x] 使用`linker.tunnel`打洞库
- [x] 使用`linker.tun`虚拟网卡库，包含`linux tun`、`windows wintun`网卡，NAT转换，网段映射
- [x] 使用`linker.snat`NAT转换库
- [x] 使用`linker.messenger.entry`集成完整功能

## 管理页面

<div align="center">
<p><img src="./readme/full.jpg"></p> 
</div>


## 支持作者

<div align="center">
请作者喝一杯咖啡，让作者更有动力更新代码
<p><img src="./readme/qr.jpg" width="360"></p> 
</div>

## 感谢投喂 

<div align="center">
<a href="https://mi-d.cn" target="_blank">
    <img src="https://mi-d.cn/wp-content/uploads/2021/12/cropped-1639494965-网站LOGO无字.png" width="40" style="vertical-align: middle;"> 米多贝克</a>
</div>


## 特别声明

本项目已加入 [dotNET China](https://gitee.com/dotnetchina)  组织。<br/>

![dotnetchina](https://images.gitee.com/uploads/images/2021/0324/120117_2da9922c_416720.png "132645_21007ea0_974299.png")