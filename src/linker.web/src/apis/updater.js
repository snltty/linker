import { sendWebsocketMsg } from './request'


export const getUpdater = (hashcode = '0') => {
    return sendWebsocketMsg('updater/get', hashcode);
}
export const confirm = (data) => {
    return sendWebsocketMsg('updater/confirm', data);
}
export const exit = (machineId) => {
    return sendWebsocketMsg('updater/exit', machineId);
}

export const setSync2Server = (data) => {
    return sendWebsocketMsg('updater/SetSync2Server', data);
}
export const setUpdateInterval = (data) => {
    return sendWebsocketMsg('updater/SetInterval', data);
}

export const getUpdaterCurrent = () => {
    return sendWebsocketMsg('updater/getcurrent');
}
export const getUpdaterServer = () => {
    return sendWebsocketMsg('updater/getserver');
}
export const getUpdaterMsg = () => {
    return sendWebsocketMsg('updater/getmsg');
}
export const confirmServer = (version) => {
    return sendWebsocketMsg('updater/confirmserver', version);
}
export const exitServer = () => {
    return sendWebsocketMsg('updater/exitserver');
}
export const subscribeUpdater = () => {
    return sendWebsocketMsg('updater/Subscribe');
}
export const checkUpdater = (data) => {
    return sendWebsocketMsg('updater/check', data);
}