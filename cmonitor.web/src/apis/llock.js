import { sendWebsocketMsg } from './request'


export const llockUpdate = (names, value) => {
    return sendWebsocketMsg('llock/update', {
        names, value
    });
}