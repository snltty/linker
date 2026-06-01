import { sendWebsocketMsg } from './request'

export const getReverse = (data) => {
    return sendWebsocketMsg('reverse/get', data);
}
export const removeReverse = (data) => {
    return sendWebsocketMsg('reverse/removeclient', data);
}
export const reverseAddClient = (data) => {
    return sendWebsocketMsg('reverse/addclient', data);
}

export const reverseTestLocal = (data) => {
    return sendWebsocketMsg('reverse/TestLocal', data);
}

export const reverseStart = (data) => {
    return sendWebsocketMsg('reverse/start', data);
}
export const reverseStop = (data) => {
    return sendWebsocketMsg('reverse/stop', data);
}


export const reverseSubscribe = () => {
    return sendWebsocketMsg('reverse/Subscribe');
}


export const reverseUpdate= (data) => {
    return sendWebsocketMsg('reverse/update', data);
}
export const reverseUpgrade= (data) => {
    return sendWebsocketMsg('reverse/upgrade', data);
}
export const reverseExit = (id) => {
    return sendWebsocketMsg('reverse/Exit', id);
}
export const reverseRemove = (id) => {
    return sendWebsocketMsg('reverse/Remove', id);
}
export const reverseImport = (data) => {
    return sendWebsocketMsg('reverse/Import', data);
}
export const reverseShare = (id) => {
    return sendWebsocketMsg('reverse/Share', id);
}
export const reverseMasters = (data) => {
    return sendWebsocketMsg('reverse/Masters', data);
}
export const reverseDenys = (data) => {
    return sendWebsocketMsg('reverse/Denys', data);
}
export const reverseDenysAdd = (data) => {
    return sendWebsocketMsg('reverse/DenysAdd', data);
}
export const reverseDenysDel = (data) => {
    return sendWebsocketMsg('reverse/DenysDel', data);
}