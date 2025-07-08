import { sendWebsocketMsg } from './request'

export const getFlows = (machineId = '') => {
    return sendWebsocketMsg('flow/GetFlows',machineId);
}
export const getMessengerFlows = (machineId = '') => {
    return sendWebsocketMsg('flow/GetMessengerFlows',machineId);
}
export const getSForwardFlows = (data) => {
    return sendWebsocketMsg('flow/GetSForwardFlows', data);
}
export const getRelayFlows = (data) => {
    return sendWebsocketMsg('flow/GetRelayFlows', data);
}
export const getCitys = () => {
    return sendWebsocketMsg('flow/GetCitys');
}
export const getStopwatch = (machineId = '') => {
    return sendWebsocketMsg('flow/GetStopwatch',machineId);
}
export const getForwardFlows = (data) => {
    return sendWebsocketMsg('flow/GetForwardFlows', data);
}
export const getSocks5Flows = (data) => {
    return sendWebsocketMsg('flow/GetSocks5Flows', data);
}
export const getTunnelFlows = (data) => {
    return sendWebsocketMsg('flow/GetTunnelFlows', data);
}