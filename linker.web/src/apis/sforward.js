import { sendWebsocketMsg } from './request'

export const getSForwardSecretKey = () => {
    return sendWebsocketMsg('sforwardclient/GetSecretKey');
}
export const setSForwardSecretKey = (data) => {
    return sendWebsocketMsg('sforwardclient/SetSecretKey', data);
}
export const getSForwardInfo = () => {
    return sendWebsocketMsg('sforwardclient/get');
}
export const getSForwardRemoteInfo = (data) => {
    return sendWebsocketMsg('sforwardclient/getremote', data);
}
export const removeSForwardInfo = (id) => {
    return sendWebsocketMsg('sforwardclient/remove', id);
}
export const addSForwardInfo = (data) => {
    return sendWebsocketMsg('sforwardclient/add', data);
}

export const testLocalSForwardInfo = () => {
    return sendWebsocketMsg('sforwardclient/TestLocal');
}