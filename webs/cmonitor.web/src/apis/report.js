import { sendWebsocketMsg } from './request'

export const reportUpdate = (names, reportType) => {
    return sendWebsocketMsg('report/update', {
        names, reportType
    }, false, 1000);
}
export const reportPing = (names) => {
    return sendWebsocketMsg('report/ping', names, false, 1000);
}