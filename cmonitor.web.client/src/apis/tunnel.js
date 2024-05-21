import { sendWebsocketMsg } from './request'

export const getTunnelTypes = () => {
    return sendWebsocketMsg('tunnel/gettypes');
}
export const updateTunnelSetServers = (servers) => {
    return sendWebsocketMsg('tunnel/SetServers', servers);
}