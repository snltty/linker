import { sendWebsocketMsg } from './request'

export const getSForward = (data) => {
    return sendWebsocketMsg('sforward/get', data);
}
export const removeSForward = (data) => {
    return sendWebsocketMsg('sforward/removeclient', data);
}
export const sforwardAddClient = (data) => {
    return sendWebsocketMsg('sforward/addclient', data);
}

export const sforwardTestLocal = (data) => {
    return sendWebsocketMsg('sforward/TestLocal', data);
}

export const sforwardStart = (data) => {
    return sendWebsocketMsg('sforward/start', data);
}
export const sforwardStop = (data) => {
    return sendWebsocketMsg('sforward/stop', data);
}


export const sforwardSubscribe = () => {
    return sendWebsocketMsg('sforward/Subscribe');
}


export const sforwardUpdate= (data) => {
    return sendWebsocketMsg('sforward/update', data);
}
export const sforwardUpgrade= (data) => {
    return sendWebsocketMsg('sforward/upgrade', data);
}
export const sforwardExit = (id) => {
    return sendWebsocketMsg('sforward/Exit', id);
}
export const sforwardRemove = (id) => {
    return sendWebsocketMsg('sforward/Remove', id);
}
export const sforwardImport = (data) => {
    return sendWebsocketMsg('sforward/Import', data);
}
export const sforwardShare = (id) => {
    return sendWebsocketMsg('sforward/Share', id);
}
export const sforwardMasters = (data) => {
    return sendWebsocketMsg('sforward/Masters', data);
}
export const sforwardDenys = (data) => {
    return sendWebsocketMsg('sforward/Denys', data);
}
export const sforwardDenysAdd = (data) => {
    return sendWebsocketMsg('sforward/DenysAdd', data);
}
export const sforwardDenysDel = (data) => {
    return sendWebsocketMsg('sforward/DenysDel', data);
}