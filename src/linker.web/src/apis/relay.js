import { sendWebsocketMsg } from './request'

export const getDefault = () => {
    return sendWebsocketMsg('relay/GetDefault');
}
export const syncDefault = (data) => {
    return sendWebsocketMsg('relay/SyncDefault', data);
}
export const setRelaySubscribe = () => {
    return sendWebsocketMsg('relay/Subscribe');
}
export const relayConnect = (data) => {
    return sendWebsocketMsg('relay/Connect', data);
}
export const relayEdit = (data) => {
    return sendWebsocketMsg('relay/edit', data);
}
export const relayExit = (id) => {
    return sendWebsocketMsg('relay/Exit', id);
}
export const relayUpdate = (id) => {
    return sendWebsocketMsg('relay/Update', id);
}