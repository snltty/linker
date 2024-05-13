import { sendWebsocketMsg } from './request'

export const getTuntapInfo = (hashcode) => {
    return sendWebsocketMsg('tuntapclient/get', hashcode);
}
export const runTuntap = (name) => {
    return sendWebsocketMsg('tuntapclient/run', name);
}
export const stopTuntap = (name) => {
    return sendWebsocketMsg('tuntapclient/stop', name);
}
export const updateTuntap = (name) => {
    return sendWebsocketMsg('tuntapclient/update', name);
}