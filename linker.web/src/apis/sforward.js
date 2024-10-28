import { sendWebsocketMsg } from './request'

export const getSForwardSecretKey = () => {
    return sendWebsocketMsg('sforwardclient/GetSecretKey');
}
export const setSForwardSecretKey = (data) => {
    return sendWebsocketMsg('sforwardclient/SetSecretKey', data);
}
export const getSForwardInfo = (data) => {
    return sendWebsocketMsg('sforwardclient/get', data);
}
export const refreshSForward = () => {
    return sendWebsocketMsg('sforwardclient/refresh');
}
export const getSForwardCountInfo = (hashcode = '0') => {
    return sendWebsocketMsg('sforwardclient/getcount', hashcode);
}
export const removeSForwardInfo = (data) => {
    return sendWebsocketMsg('sforwardclient/remove', data);
}
export const addSForwardInfo = (data) => {
    return sendWebsocketMsg('sforwardclient/add', data);
}

export const testLocalSForwardInfo = (data) => {
    return sendWebsocketMsg('sforwardclient/TestLocal', data);
}