import { sendWebsocketMsg } from './request'

export const getConfig = () => {
    return sendWebsocketMsg('configclient/get');
}

export const install = (data) => {
    return sendWebsocketMsg('configclient/install', data);
}
export const exportConfig = (data) => {
    return sendWebsocketMsg('configclient/export', data);
}

export const getAccesss = (machineid) => {
    return sendWebsocketMsg('configclient/GetAccesss', machineid);
}
export const setAccess = (data) => {
    return sendWebsocketMsg('configclient/SetAccess', data);
}
export const getSyncNames = () => {
    return sendWebsocketMsg('configclient/SyncNames');
}
export const setSync = (data) => {
    return sendWebsocketMsg('configclient/Sync', data);
}