import { sendWebsocketMsg } from './request'

export const updateVersion = (data) => {
    return sendWebsocketMsg('RunningConfig/UpdateVersion', data);
}
export const updateDisableSync = (data) => {
    return sendWebsocketMsg('RunningConfig/UpdateDisableSync', data);
}