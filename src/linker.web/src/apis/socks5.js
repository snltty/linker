import { sendWebsocketMsg } from './request'

export const getSocks5Info = (hashcode = '0') => {
    return sendWebsocketMsg('socks5/get', hashcode);
}
export const runSocks5 = (name) => {
    return sendWebsocketMsg('socks5/run', name);
}
export const stopSocks5 = (name) => {
    return sendWebsocketMsg('socks5/stop', name);
}
export const updateSocks5 = (name) => {
    return sendWebsocketMsg('socks5/update', name);
}
export const refreshSocks5 = () => {
    return sendWebsocketMsg('socks5/refresh');
}