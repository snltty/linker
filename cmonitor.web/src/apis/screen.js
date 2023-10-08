import { sendWebsocketMsg } from './request'

export const screenUpdate = (names) => {
    return sendWebsocketMsg('screen/update', names);
}
export const screenClip = (name, x, y, scale) => {
    return sendWebsocketMsg('screen/clip', {
        name,
        clip: {
            x, y, scale
        }
    });
}