import { sendWebsocketMsg } from './request'

export const getCounterInfo = (hashcode = '0') => {
    return sendWebsocketMsg('decenter/getcounter', hashcode);
}

export const refreshCounter = () => {
    return sendWebsocketMsg('decenter/refresh');
}
