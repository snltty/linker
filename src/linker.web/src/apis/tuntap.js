import { sendWebsocketMsg } from './request'


export const getTuntapRoutes = (machineid) => {
    return sendWebsocketMsg('tuntap/routes', machineid);
}

export const getTuntapInfo = (hashcode = '0') => {
    return sendWebsocketMsg('tuntap/get', hashcode);
}
export const runTuntap = (name) => {
    return sendWebsocketMsg('tuntap/run', name);
}
export const stopTuntap = (name) => {
    return sendWebsocketMsg('tuntap/stop', name);
}
export const updateTuntap = (name) => {
    return sendWebsocketMsg('tuntap/update', name);
}
export const refreshTuntap = () => {
    return sendWebsocketMsg('tuntap/refresh');
}
export const subscribePing = () => {
    return sendWebsocketMsg('tuntap/SubscribePing');
}
export const subscribeForwardTest = (machineid) => {
    return sendWebsocketMsg('tuntap/SubscribeForwardTest', machineid);
}


export const calcNetwork = (data) => {
    return sendWebsocketMsg('tuntap/CalcNetwork', data);
}
export const getNetwork = () => {
    return sendWebsocketMsg('tuntap/GetNetwork');
}
export const addNetwork = (data) => {
    return sendWebsocketMsg('tuntap/AddNetwork', data);
}

export const getid = (machineid) => {
    return sendWebsocketMsg('tuntap/getid', machineid);
}
export const setid = (data) => {
    return sendWebsocketMsg('tuntap/setid', data);
}