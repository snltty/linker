
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
#### Visual Studio 2022 LTSC 17.4.1
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
4. 仅使用 **MemoryPack** 第三方序列化库，其余均手写

<p><img src="./readme/size.jpg"></p> 

## 功能
###### 系统
- [x] 桌面捕获，捕获鼠标，**win api**
- [x] 系统信息，展示CPU，内存利用率，硬盘使用率 **win api**
- [x] 发呆时间，展示系统无操作时间 **win api**
- [x] 音量控制，音量和静音 **c++ core audio api**
- [x] 音频峰值，展示音频峰值，是否在播放音视频，及激昂程度
- [x] 系统亮度，暂不支持外界显示器 **WMI**
- [x] U 盘限制，禁用或启用U盘 **regedit**
- [x] 模拟键盘，键盘操作 **win api**
- [ ] 模拟鼠标，鼠标操作 **win api**
###### 程序
- [x] 程序限制，分为禁止打开程序，和自定检测关闭程序 **regedit**
- [x] 前景窗口，当前焦点程序捕获，手动关闭之 **win api**
- [x] 时间统计，查看程序使用时间记录
###### 网络
- [x] 网络限制，程序，域名，IP 黑白名单
- [x] 网速显示，由网络限制组件提供
###### 消息
- [x] 消息提醒，向设备发送消息提醒
- [x] 全局广播，向所有设备发送广播

###### 其它
- [x] 壁纸程序，为所有设备设置统一壁纸，以程序的方式
- [x] 键盘按键，显示键盘按键(当前键盘按键由壁纸程序提供) **win api**
- [x] 锁屏程序，打开锁屏程序，禁用键盘
- [x] 设备用户，显示当前使用设备用户姓名(请自定义向**内存共享**提供)
- [x] 发送命令，开机(shutdown -s -f -t 00)，关机(shutdown -r -f -t 00)，等等


## 面板
<p><img src="./readme/cmonitor.jpg"></p> 

## 运行参数

###### 公共的
1. **【--mode】** 运行模式 **client,server**
2. **【--report-delay】** 数据报告间隔ms **30**
3. **【--screen-delay】** 屏幕报告间隔ms **200**
4. **【--screen-scale】** 屏幕图片缩放比例 **0.2** 默认1/5

###### 客户端
1. **【--server】** 服务器ip  **192.168.1.18**
2. **【--service】** 服务端口 **1802**
3. **【--share-key】** 自定数据共享 **cmonitor/share**，每项数据长度255
4. **【--share-len】** 长度 **2550**，默认预留10项位置，0键盘KeyBoard、1壁纸Wallpaper、2锁屏LLock

###### 服务端
1. **【--web】** 管理UI端口 **1800**
2. **【--api】** 管理接口端口 **1801**
3. **【--service】** 服务端口 **1802**

## 安装示例
##### windows计划任务，客户端、服务端
```
params = " --report-delay 30 --screen-delay 200 --screen-scale 0.2";
//client
params += " --mode client --name cmonitor --server 192.168.1.18 --service 1802";
params += " --share-key cmonitor/share --share-len 2550";

//server
params = " --mode server --web 1800 --api 1801 --service 1802";

schtasks.exe /create /tn "cmonitor" /rl highest /sc ONLOGON /delay 0000:30 /tr "\"{exePath}\"{params}" /f
```
##### linux服务端 systemd
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

##### linux服务端 docker
docker镜像 snltty/cmonitor-alpine-x64 or snltty/cmonitor-alpine-arm64
```
docker run -it -d --name="cmonitor" \ 
-p 1800:1800/tcp -p 1801:1801/tcp -p 1802:1802/tcp -p 1802:1802/udp \ 
snltty/cmonitor-alpine-x64 \
--entrypoint ./cmonitor.run  --mode server --web 1800 --api 1801 --service 1802
```

## 支持作者

<div align="center">
请作者喝一杯咖啡，使其更有精力更新代码
<p><img src="./readme/qr.jpg" width="360"></p> 
</div>
