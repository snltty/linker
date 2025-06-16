import { sendWebsocketMsg } from './request'

export const getFirewall = (data) => {
    return sendWebsocketMsg('firewall/get', data);
}
export const addFirewall = (data) => {
    return sendWebsocketMsg('firewall/add', data);
}
export const removeFirewall = (data) => {
    return sendWebsocketMsg('firewall/remove', data);
}
export const stateFirewall = (data) => {
    return sendWebsocketMsg('firewall/state', data);
}
export const checkFirewall = (data) => {
    return sendWebsocketMsg('firewall/check', data);
}