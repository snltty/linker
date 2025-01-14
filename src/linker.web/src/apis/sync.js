import { sendWebsocketMsg } from './request'

export const getSyncNames = () => {
    return sendWebsocketMsg('sync/Names');
}
export const setSync = (data) => {
    return sendWebsocketMsg('sync/Sync', data);
}