import { sendWebsocketMsg } from './request'

export const getForwardConnections = (hashcode = '0') => {
    return sendWebsocketMsg('forwardclient/connections', hashcode);
}
export const removeForwardConnection = (id) => {
    return sendWebsocketMsg('forwardclient/removeconnection', id);
}
export const getForwardCountInfo = (hashcode = '0') => {
    return sendWebsocketMsg('forwardclient/getcount', hashcode);
}
export const getForwardInfo = (hashcode = '0') => {
    return sendWebsocketMsg('forwardclient/get', hashcode);
}
export const getForwardIpv4 = () => {
    return sendWebsocketMsg('forwardclient/bindips');
}
export const removeForwardInfo = (data) => {
    return sendWebsocketMsg('forwardclient/remove', data);
}
export const addForwardInfo = (data) => {
    return sendWebsocketMsg('forwardclient/add', data);
}
export const refreshForward = () => {
    return sendWebsocketMsg('forwardclient/refresh');
}

export const testTargetForwardInfo = (machineid) => {
    return sendWebsocketMsg('forwardclient/TestTarget', machineid);
}