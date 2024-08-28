import { sendWebsocketMsg } from './request'


export const save = (data) => {
    return sendWebsocketMsg('netclient/save', data);
}