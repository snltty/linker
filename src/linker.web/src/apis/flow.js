import { sendWebsocketMsg } from './request'

export const getFlows = () => {
    return sendWebsocketMsg('flow/GetFlows');
}
export const getMessengerFlows = () => {
    return sendWebsocketMsg('flow/GetMessengerFlows');
}
export const getSForwardFlows = (data) => {
    return sendWebsocketMsg('flow/GetSForwardFlows', data);
}
export const getRelayFlows = (data) => {
    return sendWebsocketMsg('flow/GetRelayFlows', data);
}