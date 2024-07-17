import { getForwardConnections, removeForwardConnection } from "@/apis/forward";
import { getTuntapConnections, removeTuntapConnection } from "@/apis/tuntap";
import { injectGlobalData } from "@/provide";
import { inject, provide, ref } from "vue";

const connectionsSymbol = Symbol();
const forwardConnectionsSymbol = Symbol();
const tuntapConnectionsSymbol = Symbol();
export const provideConnections = () => {
    const globalData = injectGlobalData();
    const connections = ref({
        showEdit: false,
        speedCache: {},
        current: ''
    });
    provide(connectionsSymbol, connections);

    const forwardConnections = ref({
        timer: 0,
        list: {},
    });
    provide(forwardConnectionsSymbol, forwardConnections);
    const _getForwardConnections = () => {
        if (globalData.value.api.connected) {
            getForwardConnections().then((res) => {
                parseConnections(res, removeForwardConnection);
                forwardConnections.value.list = res;
                forwardConnections.value.timer = setTimeout(_getForwardConnections, 1000);
            }).catch((e) => {
                forwardConnections.value.timer = setTimeout(_getForwardConnections, 1000);
            })
        } else {
            forwardConnections.value.timer = setTimeout(_getForwardConnections, 1000);
        }
    }
    const tuntapConnections = ref({
        timer: 0,
        list: {},
    });
    provide(tuntapConnectionsSymbol, tuntapConnections);
    const _getTuntapConnections = () => {
        if (globalData.value.api.connected) {
            getTuntapConnections().then((res) => {
                parseConnections(res, removeTuntapConnection);
                tuntapConnections.value.list = res;
                tuntapConnections.value.timer = setTimeout(_getTuntapConnections, 1000);
            }).catch((e) => {
                tuntapConnections.value.timer = setTimeout(_getTuntapConnections, 1000);
            })
        } else {
            tuntapConnections.value.timer = setTimeout(_getTuntapConnections, 1000);
        }
    }
    const parseConnections = (_connections, removeFunc) => {
        const caches = connections.value.speedCache;
        for (let machineId in _connections) {
            const connection = _connections[machineId];
            connection.removeFunc = removeFunc;

            const key = `${connection.RemoteMachineId}-${connection.TransactionId}`;
            const cache = caches[key] || { SendBytes: 0, ReceiveBytes: 0 };

            connection.SendBytesText = parseSpeed(connection.SendBytes - cache.SendBytes);
            connection.ReceiveBytesText = parseSpeed(connection.ReceiveBytes - cache.ReceiveBytes);

            cache.SendBytes = connection.SendBytes;
            cache.ReceiveBytes = connection.ReceiveBytes;
            caches[key] = cache;
        }
    }
    const parseSpeed = (num) => {
        let index = 0;
        while (num >= 1024) {
            num /= 1024;
            index++;
        }
        return `${num.toFixed(2)}${['B/s', 'KB/s', 'MB/s', 'GB/s', 'TB/s'][index]}`;
    }
    const handleTunnelConnections = (machineId) => {
        connections.value.current = machineId;
        connections.value.showEdit = true;
    }
    const clearConnectionsTimeout = () => {
        clearTimeout(forwardConnections.value.timer);
        clearTimeout(tuntapConnections.value.timer);
    }
    return {
        connections,
        forwardConnections, _getForwardConnections,
        tuntapConnections, _getTuntapConnections,
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