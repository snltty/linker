import { sendWebsocketMsg } from './request'

export const screenDisplay = (names, state) => {
    return sendWebsocketMsg('display/update', {
        names, state
    }, false, 1000);
}