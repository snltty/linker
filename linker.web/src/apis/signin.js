import { sendWebsocketMsg } from './request'


export const setSignIn = (data) => {
    return sendWebsocketMsg('signInclient/set', data);
}

export const setSignInServers = (servers) => {
    return sendWebsocketMsg('signInclient/setservers', servers);
}

export const getSignInfo = () => {
    return sendWebsocketMsg('signInclient/info');
}
export const setSignInOrder = (ids) => {
    return sendWebsocketMsg('signInclient/setorder', ids);
}
export const getSignInList = (data) => {
    return sendWebsocketMsg('signInclient/List', data);
}
export const getSignInIds = (data) => {
    return sendWebsocketMsg('signInclient/ids', data);
}
export const signInDel = (machineId) => {
    return sendWebsocketMsg('signInclient/del', machineId);
}

export const setSignInName = (data) => {
    return sendWebsocketMsg('signInclient/setname', data);
}
export const setSignInGroups = (data) => {
    return sendWebsocketMsg('signInclient/SetGroups', data);
}