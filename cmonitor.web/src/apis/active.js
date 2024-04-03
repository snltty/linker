import { sendWebsocketMsg } from './request'

export const getActiveTimes = (name) => {
    return sendWebsocketMsg('active/get', name);
}
export const getActiveWindows = (name) => {
    return sendWebsocketMsg('active/windows', name);
}

export const activeTimesClear = (name) => {
    return sendWebsocketMsg('active/clear', name);
}
export const activeDisallow = (names, filenames, ids1, ids2) => {
    return sendWebsocketMsg('active/disallow', {
        devices: names, data: filenames, ids1: ids1 || [], ids2: ids2 || []
    });
}


export const activeUpdate = (data) => {
    return sendWebsocketMsg('active/update', data);
}

export const activeKill = (username, pid) => {
    return sendWebsocketMsg('active/kill', { username, pid });
}