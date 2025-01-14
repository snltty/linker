import { sendWebsocketMsg } from './request'

export const getAccesss = (machineid) => {
    return sendWebsocketMsg('access/GetAccesss', machineid);
}
export const setAccess = (data) => {
    return sendWebsocketMsg('access/SetAccess', data);
}
export const refreshAccess = () => {
    return sendWebsocketMsg('access/refresh');
}
export const setApiPassword = (data) => {
    return sendWebsocketMsg('access/SetApiPassword', data);
}