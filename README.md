
<!--
 * @Author: snltty
 * @Date: 2021-08-22 14:09:03
 * @LastEditors: snltty
 * @LastEditTime: 2022-11-21 16:36:26
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.webd:\desktop\cminitor\README.md
-->
<div align="center">
<p><img src="./readme/logo.png" height="150"></p> 

# class monitor
#### Visual Studio 2022 LTSC 17.4.1
<a href="https://jq.qq.com/?_wv=1027&k=ucoIVfz4" target="_blank">QQ 群：1121552990</a>

![GitHub Repo stars](https://img.shields.io/github/stars/snltty/cminitor?style=social)
![GitHub Repo forks](https://img.shields.io/github/forks/snltty/cminitor?style=social)
[![star](https://gitee.com/snltty/cmonitor/badge/star.svg?theme=dark)](https://gitee.com/snltty/cminitor/stargazers)
[![fork](https://gitee.com/snltty/cmonitor/badge/fork.svg?theme=dark)](https://gitee.com/snltty/cminitor/members)

使用前请确保你已知其中风险

本软件仅供学习交流，请勿用于违法犯罪

</div>

## 说明
1. 这是一个粗略的局域网监控程序（说是局域网，你放外网也不是不行）
2. 桌面捕获很粗略，只是做了一个减小图片尺寸，没有做区域更新

## 看图
<p><img src="./readme/cmonitor.jpg"></p> 


## 支持平台
- [x] 客户端支持 **【windows】**
- [x] 服务端支持 **【windows】**、**【linux】**

## 运行参数

##### 1、公共参数
- [x] **【--mode】** 运行模式 **client,server**

##### 1、客户端
- [x] **【--server】** 服务器ip  **192.168.1.18**
- [x] **【--name】** 机器名 **Dns.GetHostName()**
- [ ] **【--username-key】** 用户名内存共享key，谁在用此设备 **cmonitor/username**
- [ ] **【--username-len】** 用户名内存共享长度 **255**
- [ ] **【--keyboard-key】** 键盘按键内存共享key，按下哪些按键 **cmonitor/keyboard**
- [ ] **【--keyboard-len】** 键盘按键内存共享长度 **255**
- [ ] **【--share-key】** 自定义其它数据共享 **cmonitor/share**
- [ ] **【--share-len】** 长度 **255**

##### 2、服务端
- [x] **【--web】** 管理UI端口 **1800**
- [x] **【--api】** 管理接口端口 **1801**
- [x] **【--service】** 服务端口 **1802**

## 支持作者
请作者喝一杯咖啡，使其更有精力更新代码
<p><img src="./readme/qr.jpg" width="360"></p> 
