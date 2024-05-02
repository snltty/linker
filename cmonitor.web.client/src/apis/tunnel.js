import { sendWebsocketMsg } from './request'

export const updateTunnelConnect = (machineName) => {
    return sendWebsocketMsg('tunnel/Connect', machineName);
}
