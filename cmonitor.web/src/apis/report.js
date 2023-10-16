import { sendWebsocketMsg } from './request'

export const reportUpdate = (names, reportType) => {
    return sendWebsocketMsg('report/update', {
        names, reportType
    });
}
export const reportPing = (names) => {
    return sendWebsocketMsg('report/ping', names);
}