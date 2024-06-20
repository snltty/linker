import { sendWebsocketMsg } from './request'

export const getList = (groupid) => {
    return sendWebsocketMsg('signinserver/list', groupid);
}
export const getConfig = () => {
    return sendWebsocketMsg('signinserver/config');
}
export const delDevice = (name) => {
    return sendWebsocketMsg('signinserver/del', name);
}