
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

适合教培机构计算机教室监控

</div>

## 说明
1. 这是一个粗略的局域网监控程序（说是局域网，你放外网也不是不行）
2. 使用组件式，非常方便扩展，可由 **内存共享(MemoryMappedFiles)** 提供自己定义数据
3. 内存占用小，（非定时，自动GC），linux无解
4. 使用 **MemoryPack**、**SharpDX**、**NAudio**、**RdpSession+RdpViewer**
5. 
<p><img src="./readme/size.jpg"></p> 

## 功能
###### 系统
- [x] 桌面捕获，捕获鼠标，**screen**
    - [x] 登录界面捕获和登录页面键盘输入
    - [x] 双指画面缩放
    - [ ] 区域热更新
- [x] 功能禁用，禁用各种系统功能 **system**
    - 任务栏锁定，任务栏设置，任务栏菜单
    - 任务管理器
    - 注册表编辑，组策略编辑
    - 设置，保存设置
    - 修改主题，修改壁纸，颜色外观
    - 桌面图标
    - 屏幕保护，唤醒登录，关屏锁屏
    - 关机按钮，注销按钮，锁定按钮，修改密码，切换用户
    - 安全SAS
    - 禁用U盘
- [x] 系统信息，展示CPU，内存利用率，硬盘使用率，发呆时间 **system**
- [x] 音量控制，音量和静音 **volume**
- [x] 音频峰值，展示音频峰值，是否在播放音视频，及激昂程度 **volume**
- [x] 系统亮度，暂不支持外界显示器 **light**
- [x] 模拟键盘，键盘操作，模拟ctrl+alt+delete，模拟win+l，等等 **keyboard**
- [ ] 模拟鼠标，鼠标操作 **volume**
###### 程序
- [x] 程序限制，分为禁止打开程序，和自定检测关闭程序 **active**
- [x] 前景窗口，当前焦点程序捕获，手动关闭之 **active**
- [x] 时间统计，查看程序使用时间记录 **active**
###### 网络
- [x] 网络限制，程序，域名，IP 黑白名单 **hijack**
- [x] 网速显示，由网络限制组件提供 **hijack**
- [x] 自动连接wifi **wlan**
###### 消息
- [x] 消息提醒，向设备发送消息提醒 **message**
- [x] 全局广播，向所有设备发送广播 **notify**
- [x] 语音消息，向设备发送语音消息 **message**
###### 命令
- [x] 发送命令，执行cmd命令，等等 **command**
###### 互动
- [x] 互动答题 **snatch**
- [x] 屏幕共享，以某一设备为主机，向其它设备共享屏幕，用于演示 **viewer**第一次成功连接服务端后自动恢复**
###### 壁纸
- [x] 壁纸程序，为所有设备设置统一壁纸，以程序的方式 **wallpaper**
- [x] 键盘按键，显示键盘按键(当前键盘按键由壁纸程序提供) **wallpaper**
###### 锁屏
- [x] 锁屏程序，打开锁屏程序，禁用键盘 **llock**
###### 其它
- [x] 设备用户，显示当前使用设备用户姓名 **devices**



## 面板
<p><img src="./readme/cmonitor.jpg"></p> 

## 运行参数
```
第一次运行后，在 configs/  文件夹下，会生成配置文件，可以根据需要进行修改，然后再次运行
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
-v /usr/local/cmonitor/configs:/app/configs \
snltty/cmonitor-alpine-x64
```


## 发布项目
1. nodejs 16.17.0 vue3.0 web
2. NET8.0 SDK 主程序
3. 进入根目执行
```
./publish-extends   生成web和winform
./publish  发布主程序
```
4. 在 /public/publish 目录下查看已发布程序

## 支持作者

<div align="center">
请作者喝一杯咖啡，使其更有精力更新代码
<p><img src="./readme/qr.jpg" width="360"></p> 
</div>
