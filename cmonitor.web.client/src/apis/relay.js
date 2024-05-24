import { sendWebsocketMsg } from './request'

export const getRelayTypes = () => {
    return sendWebsocketMsg('relay/gettypes');
}
export const setRelayServers = (servers) => {
    return sendWebsocketMsg('relay/SetServers', servers);
}