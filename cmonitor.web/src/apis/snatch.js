import { sendWebsocketMsg } from './request'


export const addGroup = (data) => {
    return sendWebsocketMsg('snatch/AddGroup', data);
}
export const delGroup = (username, id) => {
    return sendWebsocketMsg('snatch/DeleteGroup', {
        username, id
    });
}

export const add = (data) => {
    return sendWebsocketMsg('snatch/add', data);
}
export const del = (username, groupid, id) => {
    return sendWebsocketMsg('snatch/del', {
        username, groupid, id
    });
}

export const update = (names, question) => {
    return sendWebsocketMsg('snatch/update', {
        names, item: question
    });
}