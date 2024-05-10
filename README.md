
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

# class monitor
#### Visual Studio 2022 LTSC 17.8
<a href="https://jq.qq.com/?_wv=1027&k=ucoIVfz4" target="_blank">QQ 群：1121552990</a>

![GitHub Repo stars](https://img.shields.io/github/stars/snltty/cmonitor?style=social)
![GitHub Repo forks](https://img.shields.io/github/forks/snltty/cmonitor?style=social)
[![star](https://gitee.com/snltty/cmonitor/badge/star.svg?theme=dark)](https://gitee.com/snltty/cmonitor/stargazers)
[![fork](https://gitee.com/snltty/cmonitor/badge/fork.svg?theme=dark)](https://gitee.com/snltty/cmonitor/members)

对客户机进行监控，以及客户端组网

</div>

## 说明
1. 这是一个粗略的局域网监控程序（说是局域网，你放外网也不是不行）
2. 使用组件式，非常方便扩展，可由 **内存共享(MemoryMappedFiles)** 提供自己定义数据
3. 内存占用小，（非定时，自动GC），linux无解
4. 使用 **MemoryPack**、**SharpDX**、**NAudio**、**RdpSession+RdpViewer**、**tun2socks**
5. 为了关闭不正常的程序，使用了一些驱动（**killer.sys**），遇见报毒，请添加信任

## 功能
###### 系统
- [x] 桌面捕获，捕获鼠标，**screen**
- [x] 功能禁用，禁用各种系统功能 **system**
- [x] 音量控制，音量和静音 **volume**
- [x] 系统亮度，暂不支持外界显示器 **light**
- [x] 模拟键盘，键盘操作，模拟ctrl+alt+delete，模拟win+l，等等 **keyboard**
- [x] 发送命令，执行cmd命令，等等 **command**
###### 程序
- [x] 程序限制，分为禁止打开程序，和自定检测关闭程序 **active**
###### 网络
- [x] 网络限制，程序，域名，IP 黑白名单 **hijack**
- [x] 自动连接wifi **wlan**
###### 消息
- [x] 消息提醒，向设备发送消息提醒 **message**
- [x] 全局广播，向所有设备发送广播 **notify**
###### 互动
- [x] 互动答题 **snatch**
- [x] 屏幕共享，以某一设备为主机，向其它设备共享屏幕，用于演示 **viewer**
###### 壁纸
- [x] 壁纸程序，为所有设备设置统一壁纸，以程序的方式 **wallpaper**
###### 锁屏
- [x] 锁屏程序，打开锁屏程序，禁用键盘 **llock**
###### 打洞
- [x] 打洞连接，客户端之间打洞连接 **tunnel**
###### 中继
- [x] 中继连接，客户端之间通过服务器转发连接 **relay**
###### 组网
- [ ] 虚拟组网，使用虚拟网卡，将各个客户端组建为局域网络 **tuntap**

## 运行参数
```
第一次运行后，在 configs/  文件夹下，会生成配置文件，可以根据需要进行修改，然后再次运行

默认5个端口
    服务端 1800 web
    服务端 1801 ws管理接口
    服务端 1802 服务

    客户端 1803 web
    客户端 1804 ws管理接口

默认ws管理接口秘钥 snltty
默认分组  snltty

Common -> Modes 配置 想要哪些模式 client客户端，server服务端 

Common -> IncludePlugins 配置 只想要哪些插件 
Common -> ExcludePlugins 配置 不想要哪些插件，当不填写 IncludePlugins 时 生效
```

## 安装示例

##### 由于winform不支持裁剪程序集，所以客户端需要安装NET8.0 SDK(sdk包含runtime，最简单)

##### windows客户端、服务端
1. 可以运行 comitor.install.win.exe 进行安装操作

##### linux服务端 systemd
```
//1、下载linux版本程序，放到 /usr/local/cmonitor 文件夹，并在文件夹下创建一个 log 目录

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
```

##### linux服务端 docker
docker镜像 snltty/cmonitor-alpine-x64 or snltty/cmonitor-alpine-arm64
```
docker run -it -d --name="cmonitor" \ 
-p 1800:1800/tcp -p 1800:1800/udp \ 
-p 1801:1801/tcp -p 1801:1801/udp \ 
-p 1802:1802/tcp -p 1802:1802/udp \ 
-p 1802:1803/tcp -p 1802:1803/udp \ 
-p 1802:1804/tcp -p 1802:1804/udp \ 
-v /usr/local/cmonitor/configs:/app/configs \
snltty/cmonitor-alpine-x64
```


## 发布项目
1. arrdio
```
arrdio 发布 cmonitor.viewer.client.win
arrdio 发布 cmonitor.install.win
```
2. nodejs 16.17.0 vue3.0 web
3. NET8.0 SDK 主程序
4. 进入根目执行
```
./publish-extends   生成web和winform
./publish  发布主程序
```
5. 在 /public/publish 目录下查看已发布程序

## 支持作者

<div align="center">
请作者喝一杯咖啡，使其更有精力更新代码
<p><img src="./readme/qr.jpg" width="360"></p> 
</div>
