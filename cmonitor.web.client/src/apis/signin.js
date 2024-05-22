import { sendWebsocketMsg } from './request'

export const getConfig = () => {
    return sendWebsocketMsg('signInclient/config');
}
export const updateConfigSet = (data) => {
    return sendWebsocketMsg('signInclient/set', data);
}
export const updateConfigSetServers = (servers) => {
    return sendWebsocketMsg('signInclient/setservers', servers);
}

export const getSignInfo = () => {
    return sendWebsocketMsg('signInclient/info');
}
export const getSignList = (data) => {
    return sendWebsocketMsg('signInclient/List', data);
}
export const updateSignInDel = (machineName) => {
    return sendWebsocketMsg('signInclient/del', machineName);
}

export const updateConfigName = (data) => {
    return sendWebsocketMsg('signInclient/updatename', data);
}