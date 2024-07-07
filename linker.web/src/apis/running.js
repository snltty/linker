import { sendWebsocketMsg } from './request'

export const updateVersion = (data) => {
    return sendWebsocketMsg('RunningConfig/UpdateVersion', data);
}
