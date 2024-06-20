import { sendWebsocketMsg } from './request'

export const updateDevices = (data) => {
    return sendWebsocketMsg('devices/Update', data);
}
