import { sendWebsocketMsg } from './request'
export const meshConnect = (data) => {
    return sendWebsocketMsg('mesh/Connect', data);
}
export const meshGetNodes = (data) => {
    return sendWebsocketMsg('mesh/GetNodes', data);
}
