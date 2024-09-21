import { sendWebsocketMsg } from './request'

export const setArgs = (args) => {
    return sendWebsocketMsg('action/SetServerArgs', args);
}