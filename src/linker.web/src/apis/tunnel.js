import { sendWebsocketMsg } from './request'

export const getTunnelInfo = (hashcode = '0') => {
    return sendWebsocketMsg('tunnel/get', hashcode);
}
export const refreshTunnel = () => {
    return sendWebsocketMsg('tunnel/refresh');
}

export const setTunnelRouteLevel = (data) => {
    return sendWebsocketMsg('tunnel/SetRouteLevel', data);
}

export const getTunnelTransports = () => {
    return sendWebsocketMsg('tunnel/GetTransports');
}
export const setTunnelTransports = (data) => {
    return sendWebsocketMsg('tunnel/SetTransports', data);
}

export const getTunnelRecords = () => {
    return sendWebsocketMsg('tunnel/Records');
}