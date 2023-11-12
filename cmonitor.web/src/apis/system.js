import { sendWebsocketMsg } from './request'

export const updateRegistryOptions = (names, name, value) => {
    return sendWebsocketMsg('system/RegistryOptions', {
        names, registry: {
            name: name, value: value
        }
    });
}