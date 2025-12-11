import { sendWebsocketMsg } from './request'

export const getConfig = (data) => {
    return sendWebsocketMsg('config/get',data);
}

export const install = (data) => {
    return sendWebsocketMsg('config/install', data);
}
export const installCopy = (data) => {
    return sendWebsocketMsg('config/InstallCopy', data);
}
export const installSave = (data) => {
    return sendWebsocketMsg('config/InstallSave', data);
}
export const exportConfig = (data) => {
    return sendWebsocketMsg('config/export', data);
}
export const copyConfig = (data) => {
    return sendWebsocketMsg('config/copy', data);
}
export const saveConfig = (data) => {
    return sendWebsocketMsg('config/save', data);
}
export const shareGroup = () => {
    return sendWebsocketMsg('config/sharegroup');
}
export const joinGroup = (data) => {
    return sendWebsocketMsg('config/joingroup', data);
}