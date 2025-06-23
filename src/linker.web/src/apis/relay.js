import { sendWebsocketMsg } from './request'

export const getDefault = () => {
    return sendWebsocketMsg('relay/GetDefault');
}
export const syncDefault = (data) => {
    return sendWebsocketMsg('relay/SyncDefault', data);
}
export const setRelayServers = (servers) => {
    return sendWebsocketMsg('relay/SetServers', servers);
}
export const setRelaySubscribe = () => {
    return sendWebsocketMsg('relay/Subscribe');
}
export const relayOperating = () => {
    return sendWebsocketMsg('relay/Operating');
}
export const relayConnect = (data) => {
    return sendWebsocketMsg('relay/Connect', data);
}
export const relayUpdateNode = (data) => {
    return sendWebsocketMsg('relay/UpdateNode', data);
}
export const checkRelayKey = (key) => {
    return sendWebsocketMsg('relay/checkkey',key);
}