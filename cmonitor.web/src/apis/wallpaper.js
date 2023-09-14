import { sendWebsocketMsg } from './request'

export const wallpaperUpdate = (names, value, url = '') => {
    return sendWebsocketMsg('wallpaper/update', {
        names, value, url
    });
}