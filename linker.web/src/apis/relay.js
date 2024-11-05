import { sendWebsocketMsg } from './request'

export const setRelayServers = (servers) => {
    return sendWebsocketMsg('relay/SetServers', servers);
}
export const setRelaySubscribe = () => {
    return sendWebsocketMsg('relay/Subscribe');
}
export const relayConnect = (data) => {
    return sendWebsocketMsg('relay/Connect', data);
}