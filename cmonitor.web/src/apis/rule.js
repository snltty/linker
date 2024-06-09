import { sendWebsocketMsg } from './request'

export const getRules = () => {
    return sendWebsocketMsg('rule/info');
}

