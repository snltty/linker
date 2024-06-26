import { sendWebsocketMsg } from './request'

export const getForwardConnections = () => {
    return sendWebsocketMsg('forwardclient/connections');
}
export const removeForwardConnection = (id) => {
    return sendWebsocketMsg('forwardclient/removeconnection', id);
}

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

export const testTargetForwardInfo = (machineid) => {
    return sendWebsocketMsg('forwardclient/TestTarget', machineid);
}
export const testListenForwardInfo = () => {
    return sendWebsocketMsg('forwardclient/TestListen');
}