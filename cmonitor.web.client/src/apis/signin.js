import { sendWebsocketMsg } from './request'

export const getConfig = () => {
    return sendWebsocketMsg('signInclient/config');
}
export const setSignIn = (data) => {
    return sendWebsocketMsg('signInclient/set', data);
}
export const setSignInServers = (servers) => {
    return sendWebsocketMsg('signInclient/setservers', servers);
}

export const getSignInfo = () => {
    return sendWebsocketMsg('signInclient/info');
}
export const getSignInList = (data) => {
    return sendWebsocketMsg('signInclient/List', data);
}
export const signInDel = (machineName) => {
    return sendWebsocketMsg('signInclient/del', machineName);
}

export const setSignInName = (data) => {
    return sendWebsocketMsg('signInclient/setname', data);
}