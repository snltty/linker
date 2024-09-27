import { sendWebsocketMsg } from './request'

export const getFlows = () => {
    return sendWebsocketMsg('flowClient/GetFlows');
}
export const getMessengerFlows = () => {
    return sendWebsocketMsg('flowClient/GetMessengerFlows');
}
export const getSForwardFlows = (data) => {
    return sendWebsocketMsg('flowClient/GetSForwardFlows', data);
}
export const getRelayFlows = (data) => {
    return sendWebsocketMsg('flowClient/GetRelayFlows', data);
}