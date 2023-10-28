import { ElMessage } from 'element-plus'

let requestId = 0, ws = null, wsUrl = '', index = 1;
//请求缓存，等待回调
const requests = {};
const queues = [];
export const websocketState = { connected: false };

const sendQueueMsg = () => {
    if (queues.length > 0 && websocketState.connected && ws && ws.readyState == 1) {
        try {
            ws.send(queues.shift());
        } catch (e) { }
    }
    setTimeout(sendQueueMsg, 1000 / 60);
}
//sendQueueMsg();


const sendTimeout = () => {
    const time = Date.now();
    for (let j in requests) {
        const item = requests[j];
        if (time - item.time > item.timeout) {
            item.reject('超时~');
            delete requests[j];
        }
    }
    setTimeout(sendTimeout, 1000);
}
sendTimeout();


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

//消息处理
const onWebsocketOpen = () => {
    websocketState.connected = true;
    pushListener.push(websocketStateChangeKey, websocketState.connected);
}
const onWebsocketClose = (e) => {
    websocketState.connected = false;
    pushListener.push(websocketStateChangeKey, websocketState.connected);
    initWebsocket();
}
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
const pushMessage = (json) => {
    let callback = requests[json.RequestId];
    if (callback) {
        if (json.Code == 0) {
            callback.resolve(json.Content);
        } else if (json.Code == 1) {
            callback.reject(json.Content);
        }
        else if (json.Code == 255) {
            callback.reject(json.Content);
            if (!callback.errHandle) {
                ElMessage.error(`${callback.path}:${json.Content}`);
            }
        } else {
            pushListener.push(json.Path, json.Content);
        }
        delete requests[json.RequestId];
    } else {
        pushListener.push(json.Path, json.Content);
    }
}

export const initWebsocket = (url = wsUrl) => {
    if (ws != null) {
        ws.close();
    }
    wsUrl = url;
    ws = new WebSocket(wsUrl);
    ws.iddd = ++index;
    ws.onopen = onWebsocketOpen;
    ws.onclose = onWebsocketClose
    ws.onmessage = onWebsocketMsg
}


//发送消息
export const sendWebsocketMsg = (path, msg = {}, errHandle = false, timeout = 15000) => {
    return new Promise((resolve, reject) => {
        let id = ++requestId;
        try {
            requests[id] = { resolve, reject, errHandle, path, time: Date.now(), timeout: timeout };
            let str = JSON.stringify({
                Path: path,
                RequestId: id,
                Content: typeof msg == 'string' ? msg : JSON.stringify(msg)
            });
            if (websocketState.connected && ws.readyState == 1) {
                ws.send(str);
            } else {
                reject('网络错误~');
                //queues.push(str);
            }
        } catch (e) {
            reject('网络错误~');
            delete requests[id];
        }
    });
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


export const closeWebsocket = () => {
    if (ws) {
        ws.close();
    }
}