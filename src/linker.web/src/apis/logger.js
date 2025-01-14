import { sendWebsocketMsg } from './request'

export const getLogger = (data) => {
    return sendWebsocketMsg('logger/get', data);
}

export const clearLogger = () => {
    return sendWebsocketMsg('logger/clear');
}

export const getLoggerConfig = () => {
    return sendWebsocketMsg('logger/getconfig');
}

export const setLoggerConfig = (data) => {
    return sendWebsocketMsg('logger/setconfig', data);
}