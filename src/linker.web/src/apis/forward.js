import { sendWebsocketMsg } from './request'

export const getForwardConnections = (hashcode = '0') => {
    return sendWebsocketMsg('forward/connections', hashcode);
}
export const removeForwardConnection = (id) => {
    return sendWebsocketMsg('forward/removeconnection', id);
}
export const getForwardInfo = (hashcode = '0') => {
    return sendWebsocketMsg('forward/get', hashcode);
}
export const getForwardIpv4 = () => {
    return sendWebsocketMsg('forward/bindips');
}
export const removeForwardInfo = (data) => {
    return sendWebsocketMsg('forward/remove', data);
}
export const addForwardInfo = (data) => {
    return sendWebsocketMsg('forward/add', data);
}

export const testTargetForwardInfo = (machineid) => {
    return sendWebsocketMsg('forward/Test', machineid);
}