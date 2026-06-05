import { sendWebsocketMsg } from './request'
export const pcpConnect = (data) => {
    return sendWebsocketMsg('pcp/Connect', data);
}
export const pcpGetNodes = (data) => {
    return sendWebsocketMsg('pcp/GetNodes', data);
}
