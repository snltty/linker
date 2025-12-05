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
export const relayUpdate= (data) => {
    return sendWebsocketMsg('relay/update', data);
}
export const relayUpgrade= (data) => {
    return sendWebsocketMsg('relay/upgrade', data);
}
export const relayExit = (id) => {
    return sendWebsocketMsg('relay/Exit', id);
}
export const relayRemove = (id) => {
    return sendWebsocketMsg('relay/Remove', id);
}
export const relayImport = (data) => {
    return sendWebsocketMsg('relay/Import', data);
}
export const relayShare = (id) => {
    return sendWebsocketMsg('relay/Share', id);
}