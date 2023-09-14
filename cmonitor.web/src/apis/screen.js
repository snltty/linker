import { sendWebsocketMsg } from './request'

export const screenUpdate = (names) => {
    return sendWebsocketMsg('screen/update', names);
}