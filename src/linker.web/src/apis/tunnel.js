import { sendWebsocketMsg } from './request'

export const getTunnelInfo = (hashcode = '0') => {
    return sendWebsocketMsg('tunnel/get', hashcode);
}
export const tunnelRefresh = () => {
    return sendWebsocketMsg('tunnel/refresh');
}
export const tunnelOperating = (data) => {
    return sendWebsocketMsg('tunnel/Operating',data);
}
export const tunnelConnect = (data) => {
    return sendWebsocketMsg('tunnel/connect',data);
}

export const setTunnelRouteLevel = (data) => {
    return sendWebsocketMsg('tunnel/SetRouteLevel', data);
}

export const getTunnelTransports = (machineid) => {
    return sendWebsocketMsg('tunnel/GetTransports',machineid);
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

export const getTunnelConnections = (hashcode) => {
    return sendWebsocketMsg('channel/get',hashcode);
}
export const removeTunnelConnection = (machineid,transactionId) => {
    return sendWebsocketMsg('channel/remove',{machineid,transactionId});
}