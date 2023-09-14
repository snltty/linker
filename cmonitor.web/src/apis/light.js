import { sendWebsocketMsg } from './request'


export const setLight = (names, value) => {
    return sendWebsocketMsg('light/update', {
        names, value
    });
}