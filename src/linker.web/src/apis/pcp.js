import { sendWebsocketMsg } from './request'
export const pcpConnect = (data) => {
    return sendWebsocketMsg('pcp/Connect', data);
}
