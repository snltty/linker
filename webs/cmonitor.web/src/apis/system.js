import { sendWebsocketMsg } from './request'

export const updateRegistryOptions = (names, keys) => {
    return sendWebsocketMsg('system/RegistryOptions', {
        devices: names, data: keys
    });
}