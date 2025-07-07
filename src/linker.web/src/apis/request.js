import { ElMessage } from 'element-plus'

let requestId = 0, ws = null, wsUrl = '', index = 1, apiPassword = 'snltty';
const requests = {};
export const websocketState = { connected: false, connecting: false };

// websocket 打开
const onWebsocketOpen = () => {
    //发送密码验证
    sendWebsocketMsg('password',apiPassword || 'snltty');
}
// websocket 关闭
const onWebsocketClose = (e) => {
    websocketState.connected = false;
    websocketState.connecting = false;
    pushListener.push(websocketStateChangeKey, websocketState.connected);
    setTimeout(() => {
        initWebsocket();
    }, 1000);
}
// websocket 消息
export const onWebsocketMsg = (msg) => {
    if (typeof msg.data != 'string') {
        msg.data.arrayBuffer().then((res) => {
            const length = new DataView(res).getInt8();
            const reader = new FileReader();
            reader.readAsText(msg.data.slice(4, 4 + length), 'utf8');
            reader.onload = () => {
                let json = JSON.parse(reader.result);
                json.Content = {
                    Name: json.Content,
                    Img: msg.data.slice(4 + length, msg.data.length),
                    ArrayBuffer: res
                };
                pushMessage(json);
            }
        });
        return;
    }
    let json = JSON.parse(msg.data);
    pushMessage(json);
}
// websocket 消息处理
const pushMessage = (json) => {
    let callback = requests[json.RequestId];
    delete requests[json.RequestId];
    if (callback) {
        // 成功
        if (json.Code == 0) {
            //是密码验证
            if(json.Path == 'password' && json.Content == 'password ok'){
                websocketState.connected = true;
                websocketState.connecting = false;
                pushListener.push(websocketStateChangeKey, websocketState.connected);
            }
            //回调处理
            else{
                callback.resolve(json.Content);
            }
        } 
        // 失败
        else if (json.Code == 1) {
            callback.reject(json.Content);
        }
        // 错误
        else if (json.Code == 255) {
            callback.reject(json.Content);
            if (!callback.errHandle) {
                ElMessage.error(`${callback.path}:${json.Content}`);
            }
        } 
        // 自定义处理
        else {
            pushListener.push(json.Path, json.Content);
        }
    } 
    //没有回调，自定义处理
    else {
        pushListener.push(json.Path, json.Content);
    }
}
// 关闭websocket
export const closeWebsocket = () => {
    if (ws) {
        ws.close();
    }
}
// 发送websocket消息
export const sendWebsocketMsg = (path, msg = {}, errHandle = false, timeout = 15000) => {
    return new Promise((resolve, reject) => {
        let id = ++requestId;
        try {
            // 保存请求，用于处理超时和回调
            requests[id] = { resolve, reject, errHandle, path, time: Date.now(), timeout: timeout };

            //构建消息结构
            let str = JSON.stringify({
                Path: path,
                RequestId: id,
                Content: typeof msg == 'string' ? msg : JSON.stringify(msg)
            });
            if (ws.readyState == 1) {
                ws.send(str);
            }
        } catch (e) {
            console.log(e);
            delete requests[id];
        }
    });
}
// 初始化websocket
export const initWebsocket = (url = wsUrl, password = apiPassword) => {
    apiPassword = password;
    wsUrl = url;
    if (websocketState.connecting || websocketState.connected) {
        return;
    }
    if (ws != null) {
        ws.close();
    }
    websocketState.connecting = true;
    ws = new WebSocket(wsUrl);
    ws.iddd = ++index;
    ws.onopen = onWebsocketOpen;
    ws.onclose = onWebsocketClose
    ws.onmessage = onWebsocketMsg;
    ws.onerror = (err) => {
        ElMessage.error({ message: 'api接口连接失败，请检查接口地址或密码', grouping: true });
    };
}

//发布订阅
export const pushListener = {
    subs: {
    },
    add: function (type, callback) {
        if (typeof callback == 'function') {
            if (!this.subs[type]) {
                this.subs[type] = [];
            }
            this.subs[type].push(callback);
        }
    },
    remove(type, callback) {
        let funcs = this.subs[type] || [];
        for (let i = funcs.length - 1; i >= 0; i--) {
            if (funcs[i] == callback) {
                funcs.splice(i, 1);
            }
        }
    },
    push(type, data) {
        let funcs = this.subs[type] || [];
        for (let i = funcs.length - 1; i >= 0; i--) {
            funcs[i](data);
        }
    }
}
const websocketStateChangeKey = Symbol();
export const subWebsocketState = (callback) => {
    pushListener.add(websocketStateChangeKey, callback);
}
export const subNotifyMsg = (path, callback) => {
    pushListener.add(path, callback);
}
export const unsubNotifyMsg = (path, callback) => {
    pushListener.remove(path, callback);
}

// 请求超时处理
const sendTimeout = () => {
    const time = Date.now();
    for (let j in requests) {
        const item = requests[j];
        if (time - item.time > item.timeout) {
            item.reject(`超时:${JSON.stringify(item)}`);
            delete requests[j];
        }
    }
    setTimeout(sendTimeout, 1000);
}
sendTimeout();


