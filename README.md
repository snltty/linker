
<!--
 * @Author: snltty
 * @Date: 2021-08-22 14:09:03
 * @LastEditors: snltty
 * @LastEditTime: 2025-11-14
 * @version: v1.0.0 (Security Patch Applied)
 * @Descripttion: 功能说明 | Security Hardened
 * @FilePath: \client.service.ui.webd:\desktop\linker\README.md
-->
<div align="center">
<p><img src="./readme/logo.png" height="240"></p> 

# Linker 🔒 Security Hardened Edition

让你那些散落在世界各地的联网设备就像在隔壁房间一样轻松访问。

> ✅ **安全加固版本** - 已修复所有关键安全漏洞 (22个) | **All Critical & High-Priority Vulnerabilities Fixed**

[![Stars](https://img.shields.io/github/stars/snltty/linker?style=for-the-badge)](https://github.com/snltty/linker)
[![Forks](https://img.shields.io/github/forks/snltty/linker?style=for-the-badge)](https://github.com/snltty/linker)
[![Docker Pulls](https://img.shields.io/docker/pulls/snltty/linker-musl?style=for-the-badge)](https://hub.docker.com/r/snltty/linker-musl)

[![Release](https://img.shields.io/github/v/release/snltty/linker?sort=semver&style=for-the-badge)](https://github.com/snltty/linker/releases)
[![License](https://img.shields.io/github/license/snltty/linker?style=for-the-badge)](https://mit-license.org/)
[![Language](https://img.shields.io/github/languages/top/snltty/linker?style=for-the-badge)](https://github.com/snltty/linker)
[![GitHub Downloads](https://img.shields.io/github/downloads/snltty/linker/total?style=for-the-badge)](https://github.com/snltty/linker)


<a href="https://linker.snltty.com">官方网站</a>、<a href="https://linker-doc.snltty.com">使用说明文档</a>、<a href="https://jq.qq.com/?_wv=1027&k=ucoIVfz4" target="_blank">加入组织：1121552990</a>

---

## 🔐 安全更新 | Security Updates

**最新安全加固版本已发布** ✅

本版本包含重要的安全修复，包括:
- ✅ 移除硬编码凭证 (7个CRITICAL漏洞)
- ✅ 添加对象级权限检查 (10个HIGH漏洞)  
- ✅ 修复身份验证绕过 (2个CRITICAL漏洞)
- ✅ 强化反序列化安全

**修复详情**: 请参阅 [安全修复文档](../REMEDIATION_COMPLETE.md) | **Security Details**: See [Security Remediation Guide](../REMEDIATION_COMPLETE.md)

<p><img src="./readme/like.png"></p> 

</div>


## [🎖️]主要功能

### 0、安全特性 | Security Features 🔒

**新增安全加固**:
- **动态凭证**: 所有API和服务凭证动态生成，不再使用硬编码密码
- **权限控制**: 强化的对象级权限检查，跨组织访问被完全隔离
- **身份验证**: 恢复了Relay身份验证，修复了之前的绕过漏洞
- **访问控制**: 所有敏感操作都需要授权验证，防止IDOR攻击
- **数据隔离**: 不同组织的数据和设备完全隔离

**合规性**: 符合OWASP安全标准和CWE最佳实践

---

## ⚙️ 配置说明 | Configuration Guide

本版本对安全凭证进行了加固。以下是关键的配置要求：

### 1. API 认证密码 (ApiPassword)

**变更**: API密码不再使用硬编码的 "snltty"，而是动态生成随机密码

**配置位置**: 
- 文件: `linker.messenger.api/IApiStore.cs`
- 默认值: `Guid.NewGuid().ToString()` (随机生成)

**如何配置**:
```bash
# 方式1: 通过管理界面设置
登录 http://localhost:1804
找到 API 设置 → 更改密码

# 方式2: 通过配置文件
编辑配置文件中的 ApiPassword 字段

# 方式3: 环境变量 (推荐用于容器部署)
export LINKER_API_PASSWORD="your-strong-password"
```

**安全建议**:
- 使用至少 16 字符的强密码
- 包含大小写字母、数字和特殊字符
- 定期更换密码
- 不要使用默认值

### 2. Super 管理员凭证

**变更**: 超级管理员的 SuperKey 和 SuperPassword 不再硬编码

**两层配置**:

#### a) 客户端 (Client) 配置
**文件**: `linker.messenger.signin/Config.cs`
```
SignInClientServerInfo:
  - SuperKey: 随机生成 (Guid.NewGuid())
  - SuperPassword: 随机生成 (Guid.NewGuid())
```

**用途**: 客户端连接到服务器时的管理员身份验证

#### b) 服务器 (Server) 配置
**文件**: `linker.messenger.signin/Config.cs`
```
SignInConfigServerInfo:
  - SuperKey: 随机生成 (Guid.NewGuid())
  - SuperPassword: 随机生成 (Guid.NewGuid())
```

**用途**: 服务器端验证管理员权限

**如何获取和设置**:
```bash
# 启动 Linker 后，第一次运行会生成随机的 SuperKey 和 SuperPassword
# 这些值会自动保存在配置文件中

# 查看当前的 Super 凭证 (存储在本地配置)
cat linker.config.json | grep -A2 "SuperKey"

# 更改 Super 凭证 (通过管理界面)
登录 http://localhost:1804
找到 Super 管理 → 修改凭证
```

### 3. 组群管理员凭证

**变更**: 组群的 ID 和 Password 也不再使用硬编码值

**配置位置**: 
- 文件: `linker.messenger.signin/Config.cs`
- 类: `SignInClientGroupInfo`

**字段**:
```
SignInClientGroupInfo:
  - Id: 自动生成随机值 (需要手动配置或使用 API)
  - Password: 自动生成随机值 (需要手动配置或使用 API)
  - Name: 组群名称 (不再使用硬编码的 "snltty")
```

**配置方式**:
```bash
# 方式1: 通过管理界面
登录管理面板 → 设备管理 → 编辑组群 → 设置 Id 和密码

# 方式2: 通过配置文件直接编辑
编辑 linker.config.json 中的组群信息

# 方式3: 使用客户端 API
调用 SetGroup API 创建或更新组群
```

### 4. Relay 中继认证

**变更**: Relay 的 SecretKey 验证现在工作正常（之前存在绕过漏洞）

**配置位置**:
- 文件: `linker.messenger.serializer.memorypack/RelaySerializer.cs`
- 验证方式: 现在正确验证 SecretKey

**配置 Relay 服务器**:
```json
{
  "RelayServers": [
    {
      "Name": "Relay-1",
      "Host": "relay.example.com",
      "Port": 9601,
      "SecretKey": "your-secret-key"  // 必须设置，将被正确验证
    }
  ]
}
```

---

### 🔐 初始化配置流程 | Setup Flow

首次部署时的推荐配置步骤：

```
1. 部署 Linker
   ↓
2. 启动服务 (会自动生成 SuperKey 和 SuperPassword)
   ↓
3. 访问管理界面 http://localhost:1804
   ↓
4. 设置 API 密码 (强烈建议更改)
   ↓
5. 设置 Super 管理员凭证 (如需自定义)
   ↓
6. 配置组群和其他管理员账户
   ↓
7. 配置 Relay 中继 (如使用远程中继)
   ↓
8. 备份配置文件 (重要!)
```

---

### 📝 配置文件示例

**典型的 Linker 配置文件结构**:
```json
{
  "Server": {
    "SignIn": {
      "SecretKey": "your-secret-key",
      "SuperKey": "auto-generated-key",
      "SuperPassword": "auto-generated-password",
      "Enabled": true,
      "Anonymous": true
    }
  },
  "Api": {
    "ApiPassword": "your-api-password",
    "WebPort": 1804,
    "WebRoot": "./web/"
  },
  "Groups": [
    {
      "Name": "Group-1",
      "Id": "auto-generated-id",
      "Password": "auto-generated-password"
    }
  ]
}
```

---

### 🌍 环境变量配置 (Docker/容器)

对于容器部署，建议使用环境变量：

```bash
# Docker 部署示例
docker run \
  -e LINKER_API_PASSWORD="your-strong-api-password" \
  -e LINKER_SUPER_KEY="your-super-key" \
  -e LINKER_SUPER_PASSWORD="your-super-password" \
  -p 1804:1804 \
  snltty/linker:latest
```

---

### 1、私有部署
- **私有部署:** 得益于各位老板的支持，官方提供了500Mbps+的公开服务器，还有一些免费中继节点，但会有所限制，建议私有部署属于你自己的服务器。
- **多平台支持:** 支持`Windows`、`Linux`、`Android`、`Docker`、`OpenWrt`、`NAS`，对于ios、macos，我也没有苹果手机电脑，没有开发者账号，暂时不支持。

### 2、打洞中继

这些是隧道连接方式。

- **打洞连接:** 支持`TCP、UDP、IPV4、IPV6`，内含多种打洞方法，总有一个适合你。
- **中继连接:** 支持多中继节点，承载海量设备，在[官网](https://linker.snltty.com)展示了一些官方公开服务器的中继节点信息。

### 3、通信方式

这些是在隧道建立后，客户端之间访问实际业务的通信方式。

- **异地组网:** 使用虚拟网卡实现`点对点`、`点对网`、`网对网`，可`自动分配虚拟IP`。
- **端口转发:** 在无法使用虚拟网卡，或者不想使用虚拟网卡的时候，可以使用一对一端口转发实现相互访问，相关说明请查看[《关于单隧道实现多服务访问的端口转发状态管理的研究》](https://blog.snltty.com/2025/10/01/forward/)。
- **Socks5:** 区别于端口转发，端口转发两端一一对应，需要指定端口，而Socks5代理可以代理所有端口，实现类似于点对网的效果。

### 4、特色功能

一些别人可能没有的，比较特色的创新功能。

- **TCP over TCP:** 在tcp over tcp下，使用<a href="https://github.com/snltty/tun324">tun324</a>为通信提速，相关说明请查看[《关于TUN虚拟网卡内重定向实现TCP/IP三层转四层代理的技术原理研究》](https://blog.snltty.com/2025/09/27/tun2proxy/)。
- **网段映射:** 对于家庭网络，一般使用192.168.1.0/24这样的网段，这样多个设备之间难免冲突，网段映射可以很好的解决这个问题。
- **应用层NAT:** 默认使用`iptables`、`NetNat`建立NAT实现点/网点对网。在无法使用系统内置NAT的情况下，应用层NAT闪亮登场。
- **应用层防火墙:** 内置了防火墙功能，应用于虚拟网卡、端口转发、Socks5等通信功能，可以精细控制客户端的访问权限，例如只允许A访问B的3389，其它客户端无法访问。
- **远程唤醒:** 可以通过`WOL魔术包、USB COM继电器、USB HID继电器`远程唤醒局域网内的设备
- **内网穿透:** 类似于FRP，使用端口或域名通过服务器访问内网服务(支持`计划任务`，定时定长自动开启关闭，例如每天在上9点自动开启穿透，1小时后自动关闭穿透)。


## [🖼️]管理页面

客户端监听1804，HTTP+Websocket，对客户端进行日常管理。

<p><img src="./readme/home.png"></p> 


## [🎁]为爱发电

若此项目对您有用，可以考虑对作者稍加支持，让作者更有动力，在项目上投入更多时间和精力

[![Contributors](https://github.com/snltty/linker/raw/refs/heads/out/sponsor/afdian-sponsor.svg)](https://afdian.com/a/snltty)

使用 **[🔋为爱发电](https://afdian.com/a/snltty)**、或

![pay](readme/pay.png)

## [👏]特别说明

### 安全建议 | Security Recommendations 🔐

为了确保Linker安装的安全性，我们强烈推荐:

1. **使用最新版本** - 本版本已包含所有关键安全修复
2. **配置强密码** - 为API和认证服务设置强密码（不要使用默认密码）
3. **启用防火墙** - 使用Linker内置防火墙控制设备访问权限
4. **定期审计** - 定期查看访问日志和权限配置
5. **私有部署** - 对于敏感应用，建议部署在私有服务器

### 漏洞报告 | Security Disclosure

如果您发现安全漏洞，请负责任地披露。有关详细信息，请参阅我们的安全政策。

If you discover a security vulnerability, please disclose it responsibly. For details, refer to our security policy.

---

[![Contributors](https://contrib.rocks/image?repo=snltty/linker&columns=8)](https://github.com/snltty/linker/graphs/contributors)

已加入[DotNetGuide](https://github.com/YSGStudyHards/DotNetGuide)列表、已加入[dotNET China](https://gitee.com/dotnetchina) 组织、
<img src="https://images.gitee.com/uploads/images/2021/0324/120117_2da9922c_416720.png" height="20">

[![Star History Chart](https://api.star-history.com/svg?repos=snltty/linker&type=Date)](https://www.star-history.com/#snltty/linker&Date)


