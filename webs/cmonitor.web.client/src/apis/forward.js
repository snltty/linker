import { sendWebsocketMsg } from './request'

export const getForwardInfo = () => {
    return sendWebsocketMsg('forwardclient/get');
}
export const getForwardIpv4 = () => {
    return sendWebsocketMsg('forwardclient/bindips');
}
export const removeForwardInfo = (id) => {
    return sendWebsocketMsg('forwardclient/remove', id);
}
export const addForwardInfo = (data) => {
    return sendWebsocketMsg('forwardclient/add', data);
}
export const refreshForward = () => {
    return sendWebsocketMsg('forwardclient/refresh');
}