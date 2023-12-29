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

export const getQuestion = (name) => {
    return sendWebsocketMsg('snatch/getQuestion', name);
}
export const addQuestion = (question) => {
    return sendWebsocketMsg('snatch/addQuestion', question);
}
export const removeQuestion = (name) => {
    return sendWebsocketMsg('snatch/removeQuestion', name);
}


export const randomQuestion = (length) => {
    return sendWebsocketMsg('snatch/RandomQuestion', length);
}

export const updateQuestion = (userName, items) => {
    return sendWebsocketMsg('snatch/UpdateQuestion', {
        userName, items
    });
}

