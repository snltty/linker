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
export const removeSForwardInfo = (id) => {
    return sendWebsocketMsg('sforwardclient/remove', id);
}
export const addSForwardInfo = (data) => {
    return sendWebsocketMsg('sforwardclient/add', data);
}
