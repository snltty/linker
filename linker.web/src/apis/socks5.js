import { sendWebsocketMsg } from './request'


export const getSocks5Connections = (hashcode = '0') => {
    return sendWebsocketMsg('socks5client/connections', hashcode);
}
export const removeSocks5Connection = (id) => {
    return sendWebsocketMsg('socks5client/removeconnection', id);
}

export const getSocks5Info = (hashcode = '0') => {
    return sendWebsocketMsg('socks5client/get', hashcode);
}
export const runSocks5 = (name) => {
    return sendWebsocketMsg('socks5client/run', name);
}
export const stopSocks5 = (name) => {
    return sendWebsocketMsg('socks5client/stop', name);
}
export const updateSocks5 = (name) => {
    return sendWebsocketMsg('socks5client/update', name);
}
export const refreshSocks5 = () => {
    return sendWebsocketMsg('socks5client/refresh');
}