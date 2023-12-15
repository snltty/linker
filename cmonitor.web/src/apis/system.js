import { sendWebsocketMsg } from './request'

export const updateRegistryOptions = (names, keys, value) => {
    return sendWebsocketMsg('system/RegistryOptions', {
        names, registry: {
            keys: keys, value: value
        }
    });
}