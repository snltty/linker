import { sendWebsocketMsg } from './request'

export const getSForwardInfo = (data) => {
    return sendWebsocketMsg('sforward/get', data);
}
export const removeSForwardInfo = (data) => {
    return sendWebsocketMsg('sforward/remove', data);
}
export const addSForwardInfo = (data) => {
    return sendWebsocketMsg('sforward/add', data);
}

export const testLocalSForwardInfo = (data) => {
    return sendWebsocketMsg('sforward/TestLocal', data);
}

export const startSForwardInfo = (data) => {
    return sendWebsocketMsg('sforward/start', data);
}
export const stopSForwardInfo = (data) => {
    return sendWebsocketMsg('sforward/stop', data);
}


export const setSForwardSubscribe = () => {
    return sendWebsocketMsg('sforward/Subscribe');
}
export const sforwardEdit = (data) => {
    return sendWebsocketMsg('sforward/edit', data);
}
export const sforwardExit = (id) => {
    return sendWebsocketMsg('sforward/Exit', id);
}
export const sforwardUpdate = (id) => {
    return sendWebsocketMsg('sforward/Update', id);
}