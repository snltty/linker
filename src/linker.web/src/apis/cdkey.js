import { sendWebsocketMsg } from './request'

export const getSecretKey = () => {
    return sendWebsocketMsg('cdkey/GetSecretKey');
}
export const setSecretKey = (data) => {
    return sendWebsocketMsg('cdkey/SetSecretKey', data);
}
export const cdkeyPage = (data) => {
    return sendWebsocketMsg('cdkey/PageCdkey', data);
}
export const cdkeyAdd = (data) => {
    return sendWebsocketMsg('cdkey/AddCdkey', data);
}
export const cdkeyDel = (data) => {
    return sendWebsocketMsg('cdkey/DelCdkey', data);
}
export const cdkeyMy = (data) => {
    return sendWebsocketMsg('cdkey/MyCdkey', data);
}
export const cdkeyTest = (data) => {
    return sendWebsocketMsg('cdkey/TestCdkey', data);
}
export const cdkeyImport = (data) => {
    return sendWebsocketMsg('cdkey/ImportCdkey', data);
}