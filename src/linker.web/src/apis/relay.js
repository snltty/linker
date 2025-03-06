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
export const relayCdkeyAccess = () => {
    return sendWebsocketMsg('relay/AccessCdkey');
}
export const relayCdkeyPage = (data) => {
    return sendWebsocketMsg('relay/PageCdkey', data);
}
export const relayCdkeyAdd = (data) => {
    return sendWebsocketMsg('relay/AddCdkey', data);
}
export const relayCdkeyDel = (data) => {
    return sendWebsocketMsg('relay/DelCdkey', data);
}