import { sendWebsocketMsg } from './request'

export const getTunnelInfo = (hashcode = '0') => {
    return sendWebsocketMsg('tunnel/get', hashcode);
}
export const tunnelRefresh = () => {
    return sendWebsocketMsg('tunnel/refresh');
}
export const tunnelOperating = () => {
    return sendWebsocketMsg('tunnel/Operating');
}
export const tunnelConnect = (data) => {
    return sendWebsocketMsg('tunnel/connect',data);
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
export const getTunnelNetwork = (data) => {
    return sendWebsocketMsg('tunnel/GetNetwork',data);
}