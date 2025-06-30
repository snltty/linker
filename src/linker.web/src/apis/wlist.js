import { sendWebsocketMsg } from './request'

export const getSecretKey = () => {
    return sendWebsocketMsg('whitelist/GetSecretKey');
}
export const setSecretKey = (data) => {
    return sendWebsocketMsg('whitelist/SetSecretKey', data);
}
export const checkKey = () => {
    return sendWebsocketMsg('whitelist/CheckKey');
}
export const wlistPage = (data) => {
    return sendWebsocketMsg('whitelist/Page', data);
}
export const wlistAdd = (data) => {
    return sendWebsocketMsg('whitelist/Add', data);
}
export const wlistDel = (data) => {
    return sendWebsocketMsg('whitelist/Del', data);
}