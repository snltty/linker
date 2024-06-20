import { sendWebsocketMsg } from './request'

export const updateSnatch = (data) => {
    return sendWebsocketMsg('snatch/update', data);
}


export const getQuestion = (name) => {
    return sendWebsocketMsg('snatch/getQuestion', name);
}
export const addQuestion = (question) => {
    return sendWebsocketMsg('snatch/addQuestion', question);
}
export const updateQuestion = (userName, items) => {
    return sendWebsocketMsg('snatch/UpdateQuestion', {
        userName, items
    });
}
export const removeQuestion = (name) => {
    return sendWebsocketMsg('snatch/removeQuestion', name);
}