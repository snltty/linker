import { sendWebsocketMsg } from './request'

export const getConnections = (hashcode) => {
    return sendWebsocketMsg('connections/get', hashcode);
}
export const removeConnection = (machineId, transactionId) => {
    return sendWebsocketMsg('connections/remove', { machineId, transactionId });
}
