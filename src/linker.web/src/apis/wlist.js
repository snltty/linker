import { sendWebsocketMsg } from './request'
export const wlistPage = (data) => {
    return sendWebsocketMsg('whitelist/Page', data);
}
export const wlistAdd = (data) => {
    return sendWebsocketMsg('whitelist/Add', data);
}
export const wlistDel = (data) => {
    return sendWebsocketMsg('whitelist/Del', data);
}