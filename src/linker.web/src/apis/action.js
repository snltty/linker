import { sendWebsocketMsg } from './request'


export const getArgs = (machineid) => {
    return sendWebsocketMsg('action/GetServerArgs', machineid);
}
export const setArgs = (keyvalue) => {
    return sendWebsocketMsg('action/SetServerArgs', keyvalue);
}