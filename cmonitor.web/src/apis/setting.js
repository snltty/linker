import { sendWebsocketMsg } from './request'

export const getSetting = () => {
    return sendWebsocketMsg('setting/get');
}
export const saveSetting = (setting) => {
    return sendWebsocketMsg('setting/set', setting);
}