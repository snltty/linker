import { sendWebsocketMsg } from './request'

export const exec = (names, commands) => {
    return sendWebsocketMsg('command/exec', {
        names, commands
    });
}

export const commandStart = (name) => {
    return sendWebsocketMsg('command/commandStart', name);
}
export const commandWrite = (name, id, command) => {
    return sendWebsocketMsg('command/commandWrite', { name, write: { id, command } });
}
export const commandStop = (name, id) => {
    return sendWebsocketMsg('command/commandStop', { name, id });
}
export const commandAlive = (name, id) => {
    return sendWebsocketMsg('command/commandAlive', { name, id });
}