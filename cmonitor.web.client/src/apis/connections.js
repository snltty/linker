import { sendWebsocketMsg } from './request'

export const getConnections = (hashcode) => {
    return sendWebsocketMsg('connections/get', hashcode);
}
export const removeConnection = (machineName, transactionId) => {
    return sendWebsocketMsg('connections/remove', { machineName, transactionId });
}
