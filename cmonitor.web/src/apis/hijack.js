import { sendWebsocketMsg } from './request'

export const getRules = () => {
    return sendWebsocketMsg('hijack/info');
}
export const addName = (data) => {
    return sendWebsocketMsg('hijack/addName', data);
}
export const updateProcess = (data) => {
    return sendWebsocketMsg('hijack/UpdateProcess', data);
}

export const updateRule = (data) => {
    return sendWebsocketMsg('hijack/UpdateRule', data);
}



export const setRules = (data) => {
    return sendWebsocketMsg('hijack/UseHijackRules', data);
}