
## linker.tunnel 打洞库说明

### 说明
你需要自己实现信标服务器，用于交换打洞信息

```
nuget 安装 linker.tunnel
```

### 1、初始化
```
//实现你的适配器
ITunnelAdapter tunnelAdapter = new MyTunnelAdapter();

//获取外网端口类列表
List<ITunnelWanPort> tunnelWanPorts = new List<ITunnelWanPort>{
    new TunnelWanPortLinker(), //这是 linker项目的获取外网IP端口的类，你可以实现你自己的类
    new MyTunnelWanPort() //你自己实现的类 
};
//创建一个获取外网端口处理器
TunnelWanPortTransfer tunnelWanPortTransfer = new TunnelWanPortTransfer();
tunnelWanPortTransfer.Init(tunnelAdapter,tunnelWanPorts);

//打洞协议列表
List<ITunnelTransport> transports = new List<ITunnelTransport>{
    new TransportMsQuic(), //内置的QUIC
    new TransportTcpNutssb() //内置的TCP
};
//创建一个打洞处理器
TunnelTransfer tunnelTransfer = new TunnelTransfer();
tunnelTransfer.Init(tunnelAdapter);
```

### 2、监听打洞成功事件
```
//监听打洞成功事件
tunnelTransfer.SetConnectedCallback("你的事务名",Action<ITunnelConnection> callback);
//移除打洞成功事件
tunnelTransfer.RemoveConnectedCallback("你的事务名",Action<ITunnelConnection> callback)
```


### 3、打洞
```

//会通过  ITunnelAdapter.SendConnectBegin 发送给对方，你需要实现这个方法
tunnelTransfer.ConnectAsync("对方的名字或编号，你自己定，取决于的你信标服务器实现","事务名");

//对方收到消息，你应该调用
tunnelTransfer.OnBegin();

//打洞失败则会调用 ITunnelAdapter.SendConnectFail 发送给对方，你需要实现这个方法
//对方收到消息，你应该调用
tunnelTransfer.OnFail();

//打洞成功则会调用 ITunnelAdapter.SendConnectSuccess 发送给对方，你需要实现这个方法
//对方收到消息，你应该调用
tunnelTransfer.OnSuccess();

```
