import { sendWebsocketMsg } from './request'

export const viewerUpdate = (data) => {
    return sendWebsocketMsg('viewer/update', data);
}