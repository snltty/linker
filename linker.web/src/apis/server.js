import { sendWebsocketMsg } from './request'

export const getFlows = () => {
    return sendWebsocketMsg('serverClient/GetFlows');
}