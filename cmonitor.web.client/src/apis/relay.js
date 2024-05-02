import { sendWebsocketMsg } from './request'

export const updateRelayConnect = (machineName) => {
    return sendWebsocketMsg('relay/Connect', machineName);
}
