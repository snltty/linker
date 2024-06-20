import { sendWebsocketMsg } from './request'


export const notifyUpdate = (groupid, speed, msg, star1, star2, star3) => {
    return sendWebsocketMsg('notify/update', {
        groupid, speed, msg, star1, star2, star3
    });
}