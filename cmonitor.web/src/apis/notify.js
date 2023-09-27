import { sendWebsocketMsg } from './request'


export const notifyUpdate = (speed, msg) => {
    return sendWebsocketMsg('notify/update', {
        speed, msg
    });
}