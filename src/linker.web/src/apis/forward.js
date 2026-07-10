import { sendWebsocketMsg } from './request'

export const getForwardInfo = (hashcode = '0') => {
    return sendWebsocketMsg('forward/get', hashcode);
}

export const removeForwardInfo = (data) => {
    return sendWebsocketMsg('forward/remove', data);
}
export const addForwardInfo = (data) => {
    return sendWebsocketMsg('forward/add', data);
}

export const testTargetForwardInfo = (machineid) => {
    return sendWebsocketMsg('forward/Test', machineid);
}