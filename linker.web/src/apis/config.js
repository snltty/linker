import { sendWebsocketMsg } from './request'

export const getConfig = () => {
    return sendWebsocketMsg('configclient/get');
}

export const install = (data) => {
    return sendWebsocketMsg('configclient/install', data);
}
export const exportConfig = () => {
    return sendWebsocketMsg('configclient/export');
}