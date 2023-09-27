import { sendWebsocketMsg } from './request'

export const exec = (names, commands) => {
    return sendWebsocketMsg('command/exec', {
        names, commands
    });
}
export const keyboard = (names, key, type) => {
    return sendWebsocketMsg('command/Keyboard', {
        names, input: { key, type }
    });
}