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
export const activeDisallow = (names, filenames, ids) => {
    return sendWebsocketMsg('active/disallow', {
        usernames: names, filenames, ids: ids || []
    });
}


export const activeAddGroup = (data) => {
    return sendWebsocketMsg('active/AddGroup', data);
}
export const activeDelGroup = (username, id) => {
    return sendWebsocketMsg('active/DeleteGroup', {
        username, id
    });
}

export const activeAdd = (data) => {
    return sendWebsocketMsg('active/add', data);
}
export const activeDel = (username, groupid, id) => {
    return sendWebsocketMsg('active/del', {
        username, groupid, id
    });
}