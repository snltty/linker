import { sendWebsocketMsg } from './request'

export const getConfig = () => {
    return sendWebsocketMsg('config/get');
}

export const install = (data) => {
    return sendWebsocketMsg('config/install', data);
}
export const exportConfig = (data) => {
    return sendWebsocketMsg('config/export', data);
}
