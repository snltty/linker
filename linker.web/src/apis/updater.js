import { sendWebsocketMsg } from './request'


export const getUpdater = () => {
    return sendWebsocketMsg('updaterclient/get');
}
export const confirm = (machineId) => {
    return sendWebsocketMsg('updaterclient/confirm', machineId);
}
export const exit = (machineId) => {
    return sendWebsocketMsg('updaterclient/exit', machineId);
}