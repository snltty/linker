import { sendWebsocketMsg } from './request'

export const getSForwardSecretKey = () => {
    return sendWebsocketMsg('sforward/GetSecretKey');
}
export const setSForwardSecretKey = (data) => {
    return sendWebsocketMsg('sforward/SetSecretKey', data);
}
export const getSForwardInfo = (data) => {
    return sendWebsocketMsg('sforward/get', data);
}
export const refreshSForward = () => {
    return sendWebsocketMsg('sforward/refresh');
}
export const getSForwardCountInfo = (hashcode = '0') => {
    return sendWebsocketMsg('sforward/getcount', hashcode);
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
export const checkSForwardKey = () => {
    return sendWebsocketMsg('sforward/checkkey');
}