import { sendWebsocketMsg } from './request'

export const activeUpdate = (name) => {
    return sendWebsocketMsg('active/update', name);
}
export const activeClear = (name) => {
    return sendWebsocketMsg('active/clear', name);
}