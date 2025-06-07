import { sendWebsocketMsg } from './request'

export const getWakeup = (data) => {
    return sendWebsocketMsg('wakeup/get', data);
}
export const addWakeup = (data) => {
    return sendWebsocketMsg('wakeup/add', data);
}
export const removeWakeup = (data) => {
    return sendWebsocketMsg('wakeup/remove', data);
}
export const sendWakeup = (data) => {
    return sendWebsocketMsg('wakeup/send', data);
}
export const getWakeupComs = (data) => {
    return sendWebsocketMsg('wakeup/comnames',data);
}
export const getWakeupHids = (data) => {
    return sendWebsocketMsg('wakeup/hidids',data);
}