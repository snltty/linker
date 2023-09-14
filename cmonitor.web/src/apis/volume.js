import { sendWebsocketMsg } from './request'


export const setVolume = (names, value) => {
    return sendWebsocketMsg('volume/update', {
        names, value
    });
}
export const setVolumeMute = (names, value) => {
    return sendWebsocketMsg('volume/mute', {
        names, value
    });
}