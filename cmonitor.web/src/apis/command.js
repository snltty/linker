import { sendWebsocketMsg } from './request'

export const exec = (names, commands) => {
    return sendWebsocketMsg('command/exec', {
        names, commands
    });
}
