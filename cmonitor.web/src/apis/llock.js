import { sendWebsocketMsg } from './request'


export const llockScreen = (names, open) => {
    return sendWebsocketMsg('llock/LockScreen', {
        names, value: open
    });
}
export const lockSystem = (names) => {
    return sendWebsocketMsg('llock/LockSystem', names);
}
