import { sendWebsocketMsg } from './request'

export const getRules = () => {
    return sendWebsocketMsg('rule/info');
}
export const addName = (data) => {
    return sendWebsocketMsg('rule/addname', data);
}
