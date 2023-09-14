import { sendWebsocketMsg } from './request'

export const getList = () => {
    return sendWebsocketMsg('signin/list');
}
export const getConfig = () => {
    return sendWebsocketMsg('signin/config');
}
export const delDevice = (name) => {
    return sendWebsocketMsg('signin/del', name);
}