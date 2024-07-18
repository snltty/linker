import { sendWebsocketMsg } from './request'


export const getUpdater = () => {
    return sendWebsocketMsg('updaterclient/get');
}
export const confirm = (machineId, version) => {
    return sendWebsocketMsg('updaterclient/confirm', { machineId, version });
}
export const exit = (machineId) => {
    return sendWebsocketMsg('updaterclient/exit', machineId);
}