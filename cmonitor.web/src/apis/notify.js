import { sendWebsocketMsg } from './request'


export const notifyUpdate = (speed, msg, star) => {
    return sendWebsocketMsg('notify/update', {
        speed, msg, star
    });
}