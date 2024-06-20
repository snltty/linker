import { sendWebsocketMsg } from './request'

export const updateModes = (data) => {
    return sendWebsocketMsg('modes/update', data);
}
export const useModes = (names, data, ids1, ids2, path) => {
    return sendWebsocketMsg(path, {
        devices: names, data: data, ids1: ids1, ids2: ids2
    });
}