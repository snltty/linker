import { sendWebsocketMsg } from './request'


export const getSocks5Connections = (hashcode = '0') => {
    return sendWebsocketMsg('socks5/connections', hashcode);
}
export const removeSocks5Connection = (id) => {
    return sendWebsocketMsg('socks5/removeconnection', id);
}

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