import { sendWebsocketMsg } from './request'


export const getTuntapConnections = (hashcode = '0') => {
    return sendWebsocketMsg('tuntapclient/connections', hashcode);
}
export const removeTuntapConnection = (id) => {
    return sendWebsocketMsg('tuntapclient/removeconnection', id);
}

export const getTuntapInfo = (hashcode = '0') => {
    return sendWebsocketMsg('tuntapclient/get', hashcode);
}
export const runTuntap = (name) => {
    return sendWebsocketMsg('tuntapclient/run', name);
}
export const stopTuntap = (name) => {
    return sendWebsocketMsg('tuntapclient/stop', name);
}
export const updateTuntap = (name) => {
    return sendWebsocketMsg('tuntapclient/update', name);
}
export const refreshTuntap = () => {
    return sendWebsocketMsg('tuntapclient/refresh');
}
export const subscribePing = () => {
    return sendWebsocketMsg('tuntapclient/SubscribePing');
}
export const subscribeForwardTest = (machineid) => {
    return sendWebsocketMsg('tuntapclient/SubscribeForwardTest', machineid);
}


export const calcNetwork = (data) => {
    return sendWebsocketMsg('tuntapclient/CalcNetwork', data);
}
export const getNetwork = () => {
    return sendWebsocketMsg('tuntapclient/GetNetwork');
}
export const addNetwork = (data) => {
    return sendWebsocketMsg('tuntapclient/AddNetwork', data);
}