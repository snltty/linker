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

export const user2NodePage = (data) => {
    return sendWebsocketMsg('relay/PageUser2Node', data);
}
export const user2NodeAdd = (data) => {
    return sendWebsocketMsg('relay/AddUser2Node', data);
}
export const user2NodeDel = (data) => {
    return sendWebsocketMsg('relay/DelUser2Node', data);
}