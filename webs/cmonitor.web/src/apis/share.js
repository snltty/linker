import { sendWebsocketMsg } from './request'

export const shareUpdate = (names, item) => {
    return sendWebsocketMsg('share/update', {
        names, item
    });
}