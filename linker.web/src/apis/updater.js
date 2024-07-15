import { sendWebsocketMsg } from './request'


export const getUpdater = () => {
    return sendWebsocketMsg('updaterclient/get');
}