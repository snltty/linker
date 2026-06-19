
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

<img src="./readme/logo.png" height="150">

# Linker

[![Stars](https://img.shields.io/github/stars/snltty/linker)](https://github.com/snltty/linker)
[![Forks](https://img.shields.io/github/forks/snltty/linker)](https://github.com/snltty/linker)

[![Docker Pulls](https://img.shields.io/docker/pulls/snltty/linker-musl)](https://hub.docker.com/r/snltty/linker-musl)
[![Release](https://img.shields.io/github/v/release/snltty/linker)](https://github.com/snltty/linker/releases)
[![License](https://img.shields.io/github/license/snltty/linker)](https://mit-license.org/)
[![Language](https://img.shields.io/github/languages/top/snltty/linker)](https://github.com/snltty/linker)

 <a href="https://linker.snltty.com">🏠官方网站</a> • <a href="https://hub.docker.com/r/snltty/linker-musl">🐳Docker Hub</a> • <a href="https://linker-doc.snltty.com">📖 使用文档</a> • <a href="https://jq.qq.com/?_wv=1027&k=ucoIVfz4" target="_blank">💬加入群聊</a> • <a href="https://ifdian.net/a/snltty" target="_blank">💰赞赏支持</a>


<img src="./readme/home.jpg">


</div>

## [🎖️]主要功能

### [🌍] 私有部署

- **私有部署:** 得益于各位老板的支持，官方提供了1000Mbps+的公开服务器，还有一些免费中继节点，但会有所限制，建议私有部署属于你自己的服务器。
- **多平台支持：** 支持'Windows'、'Linux'、'Android'、'Docker'、'OpenWrt'、'NAS'、'PVE'、'LXC'、'macOS'，对于ios，我没有苹果手机，没有开发者账号，暂时不支持。

### [🤺] 打洞中继

这些是隧道连接方式。按效果最好排序分别

- **IPV6直连:** 双方都有IPV6时可用
- **UPNP直连:** 有公网IPV4时可用，UPNP、NAT-PMP、配置直连端口
- **TCP/UDP打洞:** 支持TCP/UDP打洞，多种打洞方式，成功率较高
- **Mesh网络:** 网络比较好的客户端可以为其它客户端提供中继转发连接
- **服务器中继:** 支持多中继节点，承载海量设备

### [☎️] 通信方式

这些是在隧道建立后，客户端之间访问实际业务的通信方式。

- **异地组网:** 使用虚拟网卡实现`点对点`、`点对网`、`网对网`，可`自动分配虚拟IP`。
- **端口转发:** 在无法使用虚拟网卡，或者不想使用虚拟网卡的时候，可以使用一对一端口转发实现相互访问，相关说明请查看[《关于单隧道实现多服务访问的端口转发状态管理的研究》](https://blog.snltty.com/2025/10/01/forward/)。
- **Socks5:** 区别于端口转发，端口转发两端一一对应，需要指定端口，而Socks5代理可以代理所有端口，实现类似于点对网的效果。

### [❤️] 特色功能

一些别人可能没有的，比较特色的功能。

- **TCP over TCP:** 在tcp over tcp下，使用<a href="https://github.com/snltty/tun324">tun324</a>为通信提速，相关说明请查看[《关于TUN虚拟网卡内重定向实现TCP/IP三层转四层代理的技术原理研究》](https://blog.snltty.com/2025/09/27/tun2proxy/)。
- **网段映射:** 对于家庭网络，一般使用192.168.1.0/24这样的网段，这样多个设备之间难免冲突，网段映射可以很好的解决这个问题。
- **应用层NAT:** 默认使用`iptables`、`NetNat`建立NAT实现点/网点对网。在无法使用系统内置NAT时，将会使用内置的应用曾NAT实现点/网对网
- **应用层防火墙:** 内置了防火墙功能，应用于虚拟网卡、端口转发、Socks5等通信功能，可以精细控制客户端的访问权限，例如只允许A访问B的3389，其它客户端无法访问。
- **远程唤醒:** 可以通过`WOL魔术包、USB COM继电器、USB HID继电器`远程唤醒局域网内的设备
- **内网穿透:** 类似于FRP，使用端口或域名通过服务器访问内网服务(支持`计划任务`，定时定长自动开启关闭，例如每天在上9点自动开启穿透，1小时后自动关闭穿透)。
- **子网划分:** 对虚拟网络划分下级子网，类似vlsm，但可选的主网与子网选项，通信隔离、单向通信，双向通信。
- **FEC向前纠错:** 内置强优化FEC（增强了 批量编码、源包直接输出、批量解码输出），支持策略性冗余或多倍发包，带宽换稳定，优化丢包链路
- **强优化KCP:** 内置强优化KCP(增强了 ACK Range、选择性确认、快速重传、批量 flush)，让端口转发，socks代理等功能也能使用UDP隧道，


## [🎁]为爱发电

若此项目对您有用，可以考虑稍加支持，直接施舍，或使用 **[🔋为爱发电](https://ifdian.net/a/snltty)**

![pay](readme/pay.jpg)

[![Contributors](https://github.com/snltty/linker/raw/refs/heads/sponsor/ifdian-sponsor.svg?t=f)](https://ifdian.net/a/snltty)



## [👏]特别说明

[![Contributors](https://contrib.rocks/image?repo=snltty/linker&columns=8)](https://github.com/snltty/linker/graphs/contributors)

[![Star History Chart](https://api.star-history.com/svg?repos=snltty/linker&type=Date)](https://www.star-history.com/#snltty/linker&Date)


