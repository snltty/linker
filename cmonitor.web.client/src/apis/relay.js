import { sendWebsocketMsg } from './request'

export const updateRelayConnect = (machineName) => {
    return sendWebsocketMsg('relay/Connect', machineName);
}

export const getRelayTypes = () => {
    return sendWebsocketMsg('relay/gettypes');
}
export const updateRelaySetServers = (servers) => {
    return sendWebsocketMsg('relay/SetServers', servers);
}