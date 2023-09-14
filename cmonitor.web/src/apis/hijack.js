import { sendWebsocketMsg } from './request'

export const getRules = () => {
    return sendWebsocketMsg('hijack/info');
}
export const addName = (data) => {
    return sendWebsocketMsg('hijack/addName', data);
}

export const addProcessGroup = (data) => {
    return sendWebsocketMsg('hijack/addProcessGroup', data);
}
export const deleteProcessGroup = (data) => {
    return sendWebsocketMsg('hijack/deleteProcessGroup', data);
}
export const addProcess = (data) => {
    return sendWebsocketMsg('hijack/addProcess', data);
}
export const deleteProcess = (data) => {
    return sendWebsocketMsg('hijack/deleteProcess', data);
}

export const addRule = (data) => {
    return sendWebsocketMsg('hijack/AddRule', data);
}
export const deleteRule = (data) => {
    return sendWebsocketMsg('hijack/deleteRule', data);
}

export const updateDevices = (data) => {
    return sendWebsocketMsg('hijack/UpdateDevices', data);
}

export const setRules = (data) => {
    return sendWebsocketMsg('hijack/setRules', data);
}