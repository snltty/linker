import { sendWebsocketMsg } from './request'

export const keyboard = (names, key, type) => {
    return sendWebsocketMsg('Keyboard/Keyboard', {
        names, input: { key, type }
    });
}
export const ctrlAltDelete = (names) => {
    return sendWebsocketMsg('Keyboard/CtrlAltDelete', names);
}
