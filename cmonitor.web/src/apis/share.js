import { sendWebsocketMsg } from './request'

export const shareUpdate = (name, item) => {
    return sendWebsocketMsg('share/update', {
        name, item
    });
}