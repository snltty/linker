import { sendWebsocketMsg } from './request'

export const getActiveTimes = (name) => {
    return sendWebsocketMsg('active/get', name);
}
export const activeTimesClear = (name) => {
    return sendWebsocketMsg('active/clear', name);
}
export const activeDisallow = (names, filenames, ids) => {
    return sendWebsocketMsg('active/disallow', {
        names, filenames, ids: ids || []
    });
}

export const activeAddExe = (data) => {
    return sendWebsocketMsg('active/add', data);
}
export const activeDelExe = (username, id) => {
    return sendWebsocketMsg('active/del', {
        username, id
    });
}