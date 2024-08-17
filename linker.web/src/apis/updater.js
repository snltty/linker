import { sendWebsocketMsg } from './request'


export const getUpdater = (hashcode = '0') => {
    return sendWebsocketMsg('updaterclient/get', hashcode);
}
export const confirm = (data) => {
    return sendWebsocketMsg('updaterclient/confirm', data);
}
export const exit = (machineId) => {
    return sendWebsocketMsg('updaterclient/exit', machineId);
}

export const getSecretKey = () => {
    return sendWebsocketMsg('updaterclient/GetSecretKey');
}
export const setSecretKey = (data) => {
    return sendWebsocketMsg('updaterclient/SetSecretKey', data);
}


export const getUpdaterCurrent = () => {
    return sendWebsocketMsg('updaterclient/getcurrent');
}
export const getUpdaterServer = () => {
    return sendWebsocketMsg('updaterclient/getserver');
}
export const confirmServer = (version) => {
    return sendWebsocketMsg('updaterclient/confirmserver', version);
}
export const exitServer = () => {
    return sendWebsocketMsg('updaterclient/exitserver');
}