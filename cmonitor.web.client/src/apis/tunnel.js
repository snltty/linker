import { sendWebsocketMsg } from './request'

export const getConfig = () => {
    return sendWebsocketMsg('tunnel/config');
}
export const updateConfigSet = (data) => {
    return sendWebsocketMsg('tunnel/configset', data);
}
export const updateConfigSetServers = (servers) => {
    return sendWebsocketMsg('tunnel/configsetservers', servers);
}

export const getSignInfo = () => {
    return sendWebsocketMsg('tunnel/signininfo');
}
export const updateSignInDel = (machineName) => {
    return sendWebsocketMsg('tunnel/signindel', machineName);
}