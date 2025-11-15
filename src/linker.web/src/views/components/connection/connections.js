import { getForwardConnections, removeForwardConnection } from "@/apis/forward";
import { getTuntapConnections, removeTuntapConnection } from "@/apis/tuntap";
import { getSocks5Connections, removeSocks5Connection } from "@/apis/socks5";
import { inject, provide, ref } from "vue";

const connectionsSymbol = Symbol();
const forwardConnectionsSymbol = Symbol();
const tuntapConnectionsSymbol = Symbol();
const socks5ConnectionsSymbol = Symbol();
export const provideConnections = () => {
    const connections = ref({
        showEdit: false,
        speedCache: {},
        current: '',
        currentName: '',
        hashcode: 0,
        hashcode1: 0,

        _updateRealTime: false,
        updateRealTime: (value) => {
            forwardConnections.value.hashcode = 0;
            tuntapConnections.value.hashcode = 0;
            socks5Connections.value.hashcode = 0;
            connections.value._updateRealTime = value;
        }
    });
    provide(connectionsSymbol, connections);

    const forwardConnections = ref({
        timer: 0,
        list: {},
        hashcode: 0,
    });
    provide(forwardConnectionsSymbol, forwardConnections);
    const _getForwardConnections = () => {
        clearTimeout(forwardConnections.value.timer)
        getForwardConnections(forwardConnections.value.hashcode.toString()).then((res) => {
            if (forwardConnections.value._updateRealTime == false)
                forwardConnections.value.hashcode = res.HashCode;
            if (res.List) {
                parseConnections(res.List, removeForwardConnection);
                forwardConnections.value.list = res.List;
            }

            forwardConnections.value.timer = setTimeout(_getForwardConnections, 1000);
        }).catch((e) => {
            forwardConnections.value.timer = setTimeout(_getForwardConnections, 1000);
        })
    }


    const tuntapConnections = ref({
        timer: 0,
        list: {},
        hashcode: 0,
    });
    provide(tuntapConnectionsSymbol, tuntapConnections);
    const _getTuntapConnections = () => {
        clearTimeout(tuntapConnections.value.timer)
        getTuntapConnections(tuntapConnections.value.hashcode.toString()).then((res) => {
            if (connections.value._updateRealTime == false)
                tuntapConnections.value.hashcode = res.HashCode;
            if (res.List) {
                parseConnections(res.List, removeTuntapConnection);
                tuntapConnections.value.list = res.List;
            }

            tuntapConnections.value.timer = setTimeout(_getTuntapConnections, 1000);
        }).catch((e) => {
            tuntapConnections.value.timer = setTimeout(_getTuntapConnections, 1000);
        })
    }

    const socks5Connections = ref({
        timer: 0,
        list: {},
        hashcode: 0,
    });
    provide(socks5ConnectionsSymbol, socks5Connections);
    const _getSocks5Connections = () => {
        clearTimeout(socks5Connections.value.timer)
        getSocks5Connections(socks5Connections.value.hashcode.toString()).then((res) => {
            if (connections.value._updateRealTime == false)
                socks5Connections.value.hashcode = res.HashCode;
            if (res.List) {
                parseConnections(res.List, removeSocks5Connection);
                socks5Connections.value.list = res.List;
            }

            socks5Connections.value.timer = setTimeout(_getSocks5Connections, 1000);
        }).catch((e) => {
            socks5Connections.value.timer = setTimeout(_getSocks5Connections, 1000);
        })
    }


    const parseConnections = (_connections, removeFunc) => {
        const caches = connections.value.speedCache;
        for (let machineId in _connections) {
            const connection = _connections[machineId];
            connection.removeFunc = removeFunc;

            const key = `${connection.RemoteMachineId}-${connection.TransactionId}`;
            const cache = caches[key] || { SendBytes: 0, ReceiveBytes: 0 };

            connection.SendBytesText = parseSpeed(connection.SendBytes - cache.SendBytes,'/s');
            connection.ReceiveBytesText = parseSpeed(connection.ReceiveBytes - cache.ReceiveBytes,'/s');
            connection.SendBufferRemainingText = parseSpeed(connection.SendBufferRemaining,'');
            connection.RecvBufferRemainingText = parseSpeed(connection.RecvBufferRemaining || 0,'');

            cache.SendBytes = connection.SendBytes;
            cache.ReceiveBytes = connection.ReceiveBytes;
            caches[key] = cache;
        }
    }
    const parseSpeed = (num,subfix = '') => {
        let index = 0;
        while (num >= 1024) {
            num /= 1024;
            index++;
        }
        return `${num.toFixed(2)}${['B', 'KB', 'MB', 'GB', 'TB'][index]}${subfix}`;
    }

    const handleTunnelConnections = (device) => {
        connections.value.current = device.MachineId;
        connections.value.currentName = device.MachineName;
        connections.value.showEdit = true;
    }


    const clearConnectionsTimeout = () => {
        clearTimeout(forwardConnections.value.timer);
        clearTimeout(tuntapConnections.value.timer);
        clearTimeout(socks5Connections.value.timer);
    }
    return {
        connections,
        forwardConnections, _getForwardConnections,
        tuntapConnections, _getTuntapConnections,
        socks5Connections, _getSocks5Connections,
        handleTunnelConnections, clearConnectionsTimeout
    }
}
export const useConnections = () => {
    return inject(connectionsSymbol);
}
export const useForwardConnections = () => {
    return inject(forwardConnectionsSymbol);
}
export const useTuntapConnections = () => {
    return inject(tuntapConnectionsSymbol);
}
export const useSocks5Connections = () => {
    return inject(socks5ConnectionsSymbol);
}