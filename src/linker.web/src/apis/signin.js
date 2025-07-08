import { sendWebsocketMsg } from './request'


export const setSignIn = (data) => {
    return sendWebsocketMsg('signIn/set', data);
}

export const setSignInServers = (servers) => {
    return sendWebsocketMsg('signIn/setservers', servers);
}

export const getSignInfo = () => {
    return sendWebsocketMsg('signIn/info');
}
export const setSignInOrder = (ids) => {
    return sendWebsocketMsg('signIn/setorder', ids);
}
export const getSignInList = (data) => {
    return sendWebsocketMsg('signIn/List', data);
}
export const getSignInIds = (data) => {
    return sendWebsocketMsg('signIn/ids', data);
}
export const signInDel = (machineId) => {
    return sendWebsocketMsg('signIn/del', machineId);
}

export const setSignInName = (data) => {
    return sendWebsocketMsg('signIn/setname', data);
}
export const setSignInGroups = (data) => {
    return sendWebsocketMsg('signIn/SetGroups', data);
}

export const getSignInNames = () => {
    return sendWebsocketMsg('signIn/names');
}
export const checkSignInKey = () => {
    return sendWebsocketMsg('signIn/CheckSuper');
}
export const getSignInUserIds = (name) => {
    return sendWebsocketMsg('signIn/UserIds',name);
}