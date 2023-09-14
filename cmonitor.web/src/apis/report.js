import { sendWebsocketMsg } from './request'

export const reportUpdate = (names) => {
    return sendWebsocketMsg('report/update', names);
}
export const reportPing = (names) => {
    return sendWebsocketMsg('report/ping', names);
}