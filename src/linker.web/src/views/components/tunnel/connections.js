import { inject, provide, ref } from "vue";
import { getTunnelConnections } from "@/apis/tunnel";

const connectionsSymbol = Symbol();
export const provideConnections = () => {
    const connections = ref({
        speedCache: {},

        showEdit: false,
        device:{},
        transactionId:'',

        timer: 0,
        list: null,
        hashcode: 0,

        _updateRealTime: false,
        updateRealTime: (value) => {
            connections.value.hashcode = 0;
            connections.value._updateRealTime = value;
        }
    });
    provide(connectionsSymbol, connections);

    const parseConnections = (_connections) => {
        const result = {};
        const caches = connections.value.speedCache;
        for (let transactionId in _connections) {
            for (let machineId in _connections[transactionId]) {

                const connection = _connections[transactionId][machineId];

                result[machineId] = result[machineId] || {};
                result[machineId][transactionId] = connection;

                const key = `${machineId}-${transactionId}`;
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
        return result;
    }
    const parseSpeed = (num,subfix = '') => {
        let index = 0;
        while (num >= 1024) {
            num /= 1024;
            index++;
        }
        return `${num.toFixed(2)}${['B', 'KB', 'MB', 'GB', 'TB'][index]}${subfix}`;
    }

    const connectionDataFn = () => {
        return new Promise((resolve, reject) => { 
            getTunnelConnections(connections.value.hashcode.toString()).then((res) => { 
                if (connections.value._updateRealTime == false)
                    connections.value.hashcode = res.HashCode;
                if (res.List) {
                    connections.value.list = parseConnections(res.List);
                    resolve(true);
                    return;
                }
                resolve(false);
            }).catch(() => {
                resolve(false);
            })
        });
    }
    const connectionRefreshFn = () => { 
    }
    const connectionProcessFn = (device,json) => { 
        if(!connections.value.list) return;
        Object.assign(json,{
            hook_connection: connections.value.list[device.MachineId],
            hook_connection_load:true
        });
    }

    return {
        connections, connectionDataFn, connectionProcessFn,connectionRefreshFn
    }
}
export const useConnections = () => {
    return inject(connectionsSymbol);
}
