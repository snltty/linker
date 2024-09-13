---
sidebar_position: 6
---

# 6、Action请求

:::tip[说明]
如果你设置了Action参数。则在客户端连接服务器时，发出HTTP POST请求，当获得`ok`结果时继续连接服务器，当`非ok`时断开连接，以 javascript为例，提交Action参数
```
const ws = new WebSocket(`ws://127.0.0.1:1803`, ['接口密钥']);
ws.onopen = () => {
    const arr = [
        {Key:'token',Value:'snltty',Url:'http://127.0.0.1:5141/token/verify'}
    ];
    ws.send(JSON.stringify({  
        Path:'Action/SetArgs',//设置参数的接口
        RequestId:1,  //请求id，递增即可
        Content: JSON.stringify(arr) //内容
    }));
}
``` 

![Docusaurus Plushie](./img/action.png)
:::

