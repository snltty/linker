import { sendWebsocketMsg } from './request'


export const notifyUpdate = (speed, msg, star1, star2, star3) => {
    return sendWebsocketMsg('notify/update', {
        speed, msg, star1, star2, star3
    });
}