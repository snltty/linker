import { sendWebsocketMsg } from './request'


export const usbUpdate = (names, value) => {
    return sendWebsocketMsg('usb/update', {
        names, value
    });
}