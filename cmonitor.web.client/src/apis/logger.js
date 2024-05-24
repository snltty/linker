import { sendWebsocketMsg } from './request'

export const getLogger = (data) => {
    return sendWebsocketMsg('loggerclient/get', data);
}

export const clearLogger = () => {
    return sendWebsocketMsg('loggerclient/clear');
}

export const getLoggerConfig = () => {
    return sendWebsocketMsg('loggerclient/getconfig');
}

export const setLoggerConfig = (data) => {
    return sendWebsocketMsg('loggerclient/setconfig', data);
}