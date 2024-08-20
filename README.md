
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

<a href="https://linker.snltty.com">官方网站</a>、<a href="https://linker-doc.snltty.com">使用说明文档</a>

<a href="https://jq.qq.com/?_wv=1027&k=ucoIVfz4" target="_blank">加入 QQ 群：1121552990</a>

</div>

## 主要功能

- [x] 打洞连接，客户端之间打洞连接，TCP打洞、UDP+MsQuic打洞
- [x] 打洞类库，你可以nuget安装 `linker.tunnel` 将打洞功能集成到你的项目中
- [x] 中继连接，客户端之间通过服务器转发连接
- [x] 异地组网，使用虚拟网卡，将各个客户端组建为局域网络
- [x] 网卡类库，你可以nuget安装 `linker.tun` 将tun网卡功能集成到你的项目中
- [x] 端口转发，将客户端的端口转发到其它客户端的端口
- [x] 服务器穿透，在服务器注册端口或域名，通过访问服务器端口或域名，访问内网服务 

## 打洞理论

除了`NAT4+NAT4`理论上都能通，但是也有例外，路由器可能有特殊限制，比如`SYN out, SYN in`和`ICMP Time Exceeded`
|     | NAT1<br/>(Full Cone)  | NAT2<br/>(Address-Restricted Cone)  | NAT3<br/>(Port-Restricted Cone)  | NAT4<br/>(Symmetric)  |
|  ----  | ----  | ----  | ----  | ----  |
| NAT1(Full Cone)  | √   | √  | √  | √ | 
| NAT2(Address-Restricted Cone)  | √   | √  | √  | √ | 
| NAT3(Port-Restricted Cone)  | √   | √  | √  | √ | 
| NAT4(Symmetric)  | √   | √  | √  | × | 
## 支持作者

<div align="center">
请作者喝一杯咖啡，使其更有精力更新代码
<p><img src="./readme/qr.jpg" width="360"></p> 
</div>

## 感谢支持 

<a href="https://mi-d.cn" target="_blank">
<img src="https://mi-d.cn/wp-content/uploads/2021/12/cropped-1639494965-网站LOGO无字.png" width="40" style="vertical-align: middle;"> 米多贝克</a>


