import { sendWebsocketMsg } from './request'

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
export const relayCdkeyMy = (data) => {
    return sendWebsocketMsg('relay/MyCdkey', data);
}
export const relayCdkeyTest = (data) => {
    return sendWebsocketMsg('relay/TestCdkey', data);
}
export const relayCdkeyImport = (data) => {
    return sendWebsocketMsg('relay/ImportCdkey', data);
}
export const relayUpdateNode = (data) => {
    return sendWebsocketMsg('relay/UpdateNode', data);
}