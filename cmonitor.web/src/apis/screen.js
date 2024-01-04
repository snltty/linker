import { sendWebsocketMsg } from './request'

export const screenUpdateFull = (names, type) => {
    return sendWebsocketMsg('screen/full', {
        names, type
    }, false, 1000);
}
export const screenUpdateRegion = (names) => {
    return sendWebsocketMsg('screen/region', names, false, 1000);
}
export const screenClip = (name, data) => {
    return sendWebsocketMsg('screen/clip', {
        name,
        clip: data
    }, false, 1000);
}

export const screenDisplay = (names, state) => {
    return sendWebsocketMsg('screen/display', {
        names, state
    }, false, 1000);
}

export const screenShare = (name, names) => {
    return sendWebsocketMsg('screen/share', {
        names, name
    }, false, 1000);
}
export const screenCloseShare = (name) => {
    return sendWebsocketMsg('screen/closeShare', name, false, 1000);
}
export const screenGetShare = () => {
    return sendWebsocketMsg('screen/getshare', {}, false, 1000);
}