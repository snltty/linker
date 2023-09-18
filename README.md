
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

适合教培机构计算机教室监控

</div>

## 说明
1. 这是一个粗略的局域网监控程序（说是局域网，你放外网也不是不行）
2. 桌面捕获很粗略，只是做了一个减小图片尺寸，没有做区域更新
## 平台
- [x] 客户端支持 **【windows】**
- [x] 服务端支持 **【windows】**、**【linux】**

## 看图
<p><img src="./readme/cmonitor.jpg"></p> 

## 运行参数

##### 1、公共的
- [x] **【--mode】** 运行模式 **client,server**

##### 2、客户端
- [x] **【--server】** 服务器ip  **192.168.1.18**
- [x] **【--service** 服务端口 **1802**
- [ ] **【--share-key】** 自定数据共享 **cmonitor/share**，每项数据长度255
- [ ] **【--share-len】** 长度 **2550**，默认预留10项位置，0键盘KeyBoard、1用户名UserName、2解锁Lock、3壁纸Wallpaper、4锁屏LLock

##### 3、服务端
- [x] **【--web】** 管理UI端口 **1800**
- [x] **【--api】** 管理接口端口 **1801**
- [x] **【--service】** 服务端口 **1802**

## 安装示例
##### windows计划任务
```
//client
params = " --mode client --name cmonitor --server 192.168.1.18 --service 1802 --share-key cmonitor/share --share-len 2550";

//server
params = " --mode server --web 1800 --api 1801 --service 1802";

schtasks.exe /create /tn "cmonitor" /rl highest /sc ONLOGON /delay 0000:30 /tr "\\"{exePath}\\"{params}" /f
```
##### linux服务端
```
//1、下载linux版本程序，放到 /usr/local/cmonitor 文件夹，并在文件夹下创建一个 log 目录

//3、写配置文件
vim /etc/systemd/system/cmonitor.service

[Unit]
Description=cmonitor

[Service]
WorkingDirectory=/usr/local/cmonitor
ExecStart=/usr/local/cmonitor/cmonitor --mode server --web 1800 --api 1801 --service 1802
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
```

## 支持作者
请作者喝一杯咖啡，使其更有精力更新代码
<p><img src="./readme/qr.jpg" width="360"></p> 
