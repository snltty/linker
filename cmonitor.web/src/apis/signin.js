import { sendWebsocketMsg } from './request'

export const getList = (groupid) => {
    return sendWebsocketMsg('signin/list', groupid);
}
export const getConfig = () => {
    return sendWebsocketMsg('signin/config');
}
export const delDevice = (name) => {
    return sendWebsocketMsg('signin/del', name);
}