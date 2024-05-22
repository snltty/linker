import { sendWebsocketMsg } from './request'

export const getTunnelTypes = () => {
    return sendWebsocketMsg('tunnel/gettypes');
}
export const updateTunnelSetServers = (servers) => {
    return sendWebsocketMsg('tunnel/SetServers', servers);
}

export const getTunnelInfo = (hashcode) => {
    return sendWebsocketMsg('tunnel/get', hashcode);
}
export const refreshTunnel = () => {
    return sendWebsocketMsg('tunnel/refresh');
}

export const updateTunnel = (data) => {
    return sendWebsocketMsg('tunnel/SetConfig', data);
}