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
export const wlistStatus = (type,machineId) => {
    return sendWebsocketMsg('whitelist/status', {Key:type,Value:machineId});
}
export const wlistAddOrder = (data) => {
    return sendWebsocketMsg('whitelist/addorder', data);
}
export const wlistList = (data) => {
    return sendWebsocketMsg('whitelist/list', data);
}